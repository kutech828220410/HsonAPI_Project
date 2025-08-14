using Microsoft.AspNetCore.Mvc;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using ZXing;
using ZXing.SkiaSharp; // 來自 ZXing.Net.Bindings.SkiaSharp
using HsonAPILib;
using Basic;
using System.Runtime.InteropServices; // Marshal.Copy
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace HsonWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class barcode : ControllerBase
    {
        /// <summary>
        /// 條碼解碼 API（單一條碼版 + 加速）
        /// </summary>
        /// <remarks>
        /// 呼叫此 API 可對傳入的 Base64 圖片進行一維/二維條碼解碼（只回單一條碼）。  
        /// Fast Pass（快速嘗試）失敗後才進入 Fallback（強化、反相、旋轉、ROI）。  
        /// </remarks>
        [Route("decode")]
        [HttpPost]
        public string Decode([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "decode";

            try
            {
                // 驗證 Data
                if (returnData.Data == null || string.IsNullOrWhiteSpace(returnData.Data.ToString()))
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空（需包含 Base64 圖片字串）";
                    returnData.TimeTaken = timer.ToString();
                    return returnData.JsonSerializationt();
                }

                // 取出 Base64 部分
                string base64 = returnData.Data.ToString();
                int commaIndex = base64.IndexOf(',');
                if (commaIndex >= 0) base64 = base64.Substring(commaIndex + 1);

                // Base64 轉 byte[]
                byte[] imageBytes;
                try
                {
                    imageBytes = Convert.FromBase64String(base64);
                }
                catch
                {
                    returnData.Code = -200;
                    returnData.Result = "Base64 格式不正確（解析失敗）";
                    returnData.TimeTaken = timer.ToString();
                    return returnData.JsonSerializationt();
                }

                // SkiaSharp 解碼
                using var origin = SKBitmap.Decode(imageBytes);
                if (origin == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "無法解析圖片（格式不支援或資料損毀）";
                    returnData.TimeTaken = timer.ToString();
                    return returnData.JsonSerializationt();
                }

                // 條碼解碼（兩段式，僅取單一結果）
                var outputList = BarcodeDecoder.Decode(origin);

                if (outputList == null || outputList.Count == 0)
                {
                    returnData.Code = 404;
                    returnData.Result = "未偵測到條碼";
                    returnData.TimeTaken = timer.ToString();
                    return returnData.JsonSerializationt();
                }

                if (outputList.Count > 1)
                {
                    // 理論上不會發生（DecodeSingle 流程），保險起見仍處理
                    returnData.Code = 409;
                    returnData.Result = $"偵測到多筆條碼（{outputList.Count} 筆）。請確保畫面僅有單一條碼後再嘗試。";
                    returnData.TimeTaken = timer.ToString();
                    return returnData.JsonSerializationt();
                }

                // 成功回傳（單一條碼）
                returnData.Code = 200;
                returnData.Result = "共解碼 1 筆條碼";
                returnData.Data = outputList[0]; // 注意：回傳單一物件
                returnData.TimeTaken = timer.ToString();
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = "系統錯誤: " + ex.Message;
                returnData.TimeTaken = timer.ToString();
                return returnData.JsonSerializationt();
            }
        }


        // 二進位上傳版（multipart/form-data）
        [Route("decode-file")]
        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 50_000_000)] // 50MB，可自行調整
        [RequestSizeLimit(50_000_000)]
        public async Task<string> DecodeFile([FromForm] IFormFile image, [FromForm] string? ServerName, [FromForm] string? ServerType)
        {
            var timer = new MyTimerBasic();
            var rd = new returnData
            {
                Method = "decode",
                ServerName = ServerName,
                ServerType = ServerType
            };

            try
            {
                if (image == null || image.Length == 0)
                {
                    rd.Code = -200;
                    rd.Result = "未收到圖片（form 欄位名稱請用 image）";
                    rd.TimeTaken = timer.ToString();
                    return rd.JsonSerializationt();
                }

                // 直接以 Stream 讀進 Skia，不需轉 Base64
                using var s = image.OpenReadStream();
                using var skStream = new SKManagedStream(s, true);
                using var origin = SKBitmap.Decode(skStream);

                if (origin == null)
                {
                    rd.Code = -200;
                    rd.Result = "無法解析圖片（格式不支援或資料損毀）";
                    rd.TimeTaken = timer.ToString();
                    return rd.JsonSerializationt();
                }

                // 解碼（沿用你現有的兩段式加速 + 四點外框）
                var outputList = BarcodeDecoder.Decode(origin);

                if (outputList == null || outputList.Count == 0)
                {
                    rd.Code = 404;
                    rd.Result = "未偵測到條碼";
                    rd.TimeTaken = timer.ToString();
                    return rd.JsonSerializationt();
                }

                if (outputList.Count > 1)
                {
                    // 單一條碼模式：偵測到多筆直接回 409（若要自動挑一筆可改成 PickBestSingle）
                    rd.Code = 409;
                    rd.Result = $"偵測到多筆條碼（{outputList.Count} 筆）。請確保畫面僅有單一條碼後再嘗試。";
                    rd.TimeTaken = timer.ToString();
                    return rd.JsonSerializationt();
                }

                rd.Code = 200;
                rd.Result = "共解碼 1 筆條碼";
                rd.Data = outputList[0]; // 單一物件
                rd.TimeTaken = timer.ToString();
                return rd.JsonSerializationt();
            }
            catch (Exception ex)
            {
                rd.Code = -200;
                rd.Result = "系統錯誤: " + ex.Message;
                rd.TimeTaken = timer.ToString();
                return rd.JsonSerializationt();
            }
        }

        // ================= DTO（若已定義可刪） =================

        public class DecodeResultDto
        {
            public string Text { get; set; }
            public string Format { get; set; }
            public bool Is2D { get; set; }
            public List<DecodePoint> Points { get; set; }
        }

        public class DecodePoint
        {
            public float X { get; set; }
            public float Y { get; set; }
        }

        // 可選：若有需要
        public class BarcodeRequest
        {
            public string ImageBase64 { get; set; }
        }

        // ================= 解碼器（兩段式 + 無 unsafe） =================

        public static class BarcodeDecoder
        {
            /// <summary>
            /// 從 Base64 圖片字串解碼條碼（可含 data URL 前綴）
            /// </summary>
            public static List<DecodeResultDto> DecodeFromBase64(string base64)
            {
                if (string.IsNullOrWhiteSpace(base64))
                    return new List<DecodeResultDto>();

                int comma = base64.IndexOf(',');
                if (comma >= 0) base64 = base64.Substring(comma + 1);

                byte[] bytes;
                try { bytes = Convert.FromBase64String(base64); }
                catch { return new List<DecodeResultDto>(); }

                using (var bmp = SKBitmap.Decode(bytes))
                {
                    if (bmp == null) return new List<DecodeResultDto>();
                    return Decode(bmp);
                }
            }

            /// <summary>
            /// 解碼（兩段式，僅取單一結果；未命中回空清單）
            /// </summary>
            public static List<DecodeResultDto> Decode(SKBitmap source)
            {
                if (source == null) return new List<DecodeResultDto>();

                // 1) Fast Pass（最便宜）：1000px、0°→90°，不反相、不TryHarder
                using (var fastImg = ResizeLongest(source, 1000, SKFilterQuality.None))
                using (var fastGray = QuickGray(fastImg))
                {
                    var fastReader = BuildReader(tryHarder: false, tryInverted: false);

                    var r0 = fastReader.Decode(fastGray);
                    if (r0 != null) return ToDtos(new[] { r0 });

                    using (var fastRot90 = Rotate(fastGray, 90))
                    {
                        var r90 = fastReader.Decode(fastRot90);
                        if (r90 != null) return ToDtos(new[] { r90 });
                    }
                }

                // 2) Fallback（昂貴）：1400px、強化、反相、0/±90°、ROI
                using (var bigImg = ResizeLongest(source, 1400, SKFilterQuality.Medium))
                {
                    // 2-1) 全圖強化（適中）
                    using (var prepNormal = Preprocess(bigImg, strong: false))
                    {
                        var r = TryDecodeSingle(prepNormal, tryInverted: false, tryHarder: true);
                        if (r != null) return ToDtos(new[] { r });

                        r = TryDecodeSingle(prepNormal, tryInverted: true, tryHarder: true);
                        if (r != null) return ToDtos(new[] { r });
                    }

                    // 2-2) ROI（中央 60%）+ 強化（強）
                    using (var roi = CropRelative(bigImg, 0.2f, 0.2f, 0.6f, 0.6f))
                    {
                        if (roi != null)
                        {
                            using (var prepRoi = Preprocess(roi, strong: true))
                            {
                                var r = TryDecodeSingle(prepRoi, tryInverted: false, tryHarder: true);
                                if (r != null) return ToDtos(new[] { r });

                                r = TryDecodeSingle(prepRoi, tryInverted: true, tryHarder: true);
                                if (r != null) return ToDtos(new[] { r });
                            }
                        }
                    }
                }

                return new List<DecodeResultDto>(); // 未命中
            }

            // ================= 內部工具 =================

            /// <summary>建立條碼讀取器（關 AutoRotate，格式收斂）</summary>
            private static BarcodeReader BuildReader(bool tryHarder, bool tryInverted)
            {
                return new BarcodeReader
                {
                    Options = new ZXing.Common.DecodingOptions
                    {
                        TryHarder = tryHarder,
                        TryInverted = tryInverted,
                        PureBarcode = false,
                        // 只保留常用格式（依需求調整）
                        PossibleFormats = new List<BarcodeFormat>
                        {
                            BarcodeFormat.QR_CODE,
                            BarcodeFormat.CODE_128,
                            BarcodeFormat.EAN_13,
                            BarcodeFormat.EAN_8,
                            BarcodeFormat.ITF
                        }
                    },
                    AutoRotate = false,
                    TryInverted = tryInverted
                };
            }

            /// <summary>單一結果：依 0°、+90°、-90° 試，命中立即回傳</summary>
            private static Result TryDecodeSingle(SKBitmap bmp, bool tryInverted, bool tryHarder)
            {
                var reader = BuildReader(tryHarder, tryInverted);

                // 0°
                var r = reader.Decode(bmp);
                if (r != null) return r;

                // +90°
                using (var r90 = Rotate(bmp, 90))
                {
                    r = reader.Decode(r90);
                    if (r != null) return r;
                }

                // -90°
                using (var rn90 = Rotate(bmp, -90))
                {
                    r = reader.Decode(rn90);
                    if (r != null) return r;
                }

                return null;
            }

            /// <summary>
            /// 統一輸出四點外框：
            /// 一維碼由兩端點推矩形，二維碼由所有點計算最小外接矩形
            /// </summary>
            private static List<DecodeResultDto> ToDtos(Result[] results)
            {
                var list = new List<DecodeResultDto>();
                for (int i = 0; i < results.Length; i++)
                {
                    var r = results[i];
                    var dto = new DecodeResultDto
                    {
                        Text = r.Text ?? "",
                        Format = r.BarcodeFormat.ToString(),
                        Is2D = Is2DFormat(r.BarcodeFormat),
                        Points = new List<DecodePoint>()
                    };

                    var pts = r.ResultPoints ?? Array.Empty<ResultPoint>();

                    if (!dto.Is2D)
                    {
                        // 一維碼：用兩端點轉四點
                        if (pts.Length >= 2)
                            dto.Points = ToQuadFromTwo(pts[0], pts[1], 0f); // thickness=0 自動估算
                        else
                            dto.Points = ToQuadFromAny(pts, 0f); // 防呆
                    }
                    else
                    {
                        // 二維碼：任意點集計算外接矩形
                        dto.Points = ToQuadFromAny(pts, 0f);
                    }

                    list.Add(dto);
                }
                return list;
            }

            private static bool Is2DFormat(BarcodeFormat f)
            {
                return f == BarcodeFormat.QR_CODE ||
                       f == BarcodeFormat.DATA_MATRIX ||
                       f == BarcodeFormat.AZTEC ||
                       f == BarcodeFormat.PDF_417;
            }

            /// <summary>快速灰階（只轉色，不做強化）</summary>
            private static SKBitmap QuickGray(SKBitmap src)
            {
                var gray = new SKBitmap(src.Width, src.Height, SKColorType.Gray8, SKAlphaType.Opaque);
                using (var canvas = new SKCanvas(gray))
                using (var paint = new SKPaint())
                {
                    float[] mat = {
                        0.299f, 0.299f, 0.299f, 0, 0,
                        0.587f, 0.587f, 0.587f, 0, 0,
                        0.114f, 0.114f, 0.114f, 0, 0,
                        0,      0,      0,      1, 0
                    };
                    paint.ColorFilter = SKColorFilter.CreateColorMatrix(mat);
                    canvas.DrawBitmap(src, 0, 0, paint);
                }
                return gray;
            }

            /// <summary>預處理：灰階 + 對比 LUT（strong 控制強度）</summary>
            private static SKBitmap Preprocess(SKBitmap src, bool strong)
            {
                // 轉 Rgba8888
                SKBitmap work;
                if (src.ColorType == SKColorType.Rgba8888)
                    work = src.Copy();
                else
                {
                    var info = new SKImageInfo(src.Width, src.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
                    work = new SKBitmap(info);
                    using (var canvas = new SKCanvas(work)) canvas.DrawBitmap(src, 0, 0);
                }

                // 灰階
                var gray = new SKBitmap(work.Width, work.Height, SKColorType.Gray8, SKAlphaType.Opaque);
                using (var canvas = new SKCanvas(gray))
                using (var paint = new SKPaint())
                {
                    float[] mat = {
                        0.299f, 0.299f, 0.299f, 0, 0,
                        0.587f, 0.587f, 0.587f, 0, 0,
                        0.114f, 0.114f, 0.114f, 0, 0,
                        0,      0,      0,      1, 0
                    };
                    paint.ColorFilter = SKColorFilter.CreateColorMatrix(mat);
                    canvas.DrawBitmap(work, 0, 0, paint);
                }
                work.Dispose();

                // 對比 LUT（無 unsafe）
                byte[] lut = BuildContrastLut(strong ? 1.8f : 1.5f);
                using (var pix = gray.PeekPixels())
                {
                    IntPtr ptr = pix.GetPixels();
                    int len = gray.Width * gray.Height;
                    byte[] buf = new byte[len];
                    Marshal.Copy(ptr, buf, 0, len);

                    for (int i = 0; i < len; i++)
                        buf[i] = lut[buf[i]];

                    Marshal.Copy(buf, 0, ptr, len);
                }

                return gray;
            }

            /// <summary>對比 LUT</summary>
            private static byte[] BuildContrastLut(float C)
            {
                byte[] lut = new byte[256];
                for (int i = 0; i < 256; i++)
                {
                    float v = (float)i / 255f;
                    v = (v - 0.5f) * C + 0.5f;
                    if (v < 0f) v = 0f; else if (v > 1f) v = 1f;
                    lut[i] = (byte)(v * 255f);
                }
                return lut;
            }

            /// <summary>影像 Resize（可指定濾鏡）</summary>
            private static SKBitmap ResizeLongest(SKBitmap src, int targetLongest, SKFilterQuality q)
            {
                int w = src.Width, h = src.Height;
                int longest = Math.Max(w, h);
                if (longest <= targetLongest) return src.Copy();

                float scale = (float)targetLongest / (float)longest;
                var info = new SKImageInfo((int)(w * scale), (int)(h * scale), SKColorType.Rgba8888, SKAlphaType.Premul);
                var resized = new SKBitmap(info);
                using (var canvas = new SKCanvas(resized))
                using (var paint = new SKPaint { FilterQuality = q })
                    canvas.DrawBitmap(src, new SKRect(0, 0, w, h), new SKRect(0, 0, info.Width, info.Height), paint);

                return resized;
            }

            /// <summary>旋轉（只在需要時建立暫圖）</summary>
            private static SKBitmap Rotate(SKBitmap bmp, int angleDegrees)
            {
                bool swap = (Math.Abs(angleDegrees) % 180) != 0;
                int tw = swap ? bmp.Height : bmp.Width;
                int th = swap ? bmp.Width : bmp.Height;

                var dst = new SKBitmap(new SKImageInfo(tw, th, bmp.ColorType, bmp.AlphaType));
                using (var canvas = new SKCanvas(dst))
                {
                    canvas.Translate(tw / 2f, th / 2f);
                    canvas.RotateDegrees(angleDegrees);
                    canvas.Translate(-bmp.Width / 2f, -bmp.Height / 2f);
                    canvas.DrawBitmap(bmp, 0, 0);
                }
                return dst;
            }

            /// <summary>相對裁切（x,y,w,h 皆用比例）</summary>
            private static SKBitmap CropRelative(SKBitmap src, float xRatio, float yRatio, float wRatio, float hRatio)
            {
                int x = (int)(src.Width * xRatio);
                int y = (int)(src.Height * yRatio);
                int w = (int)(src.Width * wRatio);
                int h = (int)(src.Height * hRatio);
                if (w <= 0 || h <= 0) return null;

                var dst = new SKBitmap(new SKImageInfo(w, h, src.ColorType, src.AlphaType));
                using (var canvas = new SKCanvas(dst))
                {
                    var s = new SKRect(x, y, x + w, y + h);
                    var d = new SKRect(0, 0, w, h);
                    canvas.DrawBitmap(src, s, d);
                }
                return dst;
            }
            /// <summary>
            /// 由一維碼的兩個端點生成四點外框（依條碼方向取法向量，對稱擴張厚度）
            /// thickness<=0 時自動依長度比例計算並夾在合理範圍
            /// </summary>
            private static List<DecodePoint> ToQuadFromTwo(ResultPoint p1, ResultPoint p2, float thickness = 0f)
            {
                float dx = p2.X - p1.X;
                float dy = p2.Y - p1.Y;
                float len = (float)Math.Sqrt(dx * dx + dy * dy);
                if (len < 1f)
                {
                    // 太短時給一個小矩形避免除以 0
                    len = 1f;
                    dx = 1f; dy = 0f;
                }

                if (thickness <= 0f)
                {
                    // 依條碼長度比例推厚度，再夾在 8~60px 之間（可調）
                    thickness = Math.Clamp(len * 0.18f, 8f, 60f);
                }

                // 單位法向量（垂直於條碼方向）
                float ux = -dy / len;
                float uy = dx / len;

                float ox = ux * (thickness / 2f);
                float oy = uy * (thickness / 2f);

                // 四個角點：上邊兩點（往 -offset），下邊兩點（往 +offset）
                var topLeft = new DecodePoint { X = p1.X - ox, Y = p1.Y - oy };
                var topRight = new DecodePoint { X = p2.X - ox, Y = p2.Y - oy };
                var bottomRight = new DecodePoint { X = p2.X + ox, Y = p2.Y + oy };
                var bottomLeft = new DecodePoint { X = p1.X + ox, Y = p1.Y + oy };

                return new List<DecodePoint> { topLeft, topRight, bottomRight, bottomLeft };
            }
            /// <summary>
            /// 將任意點集轉為外接矩形四點（順時針）
            /// </summary>
            private static List<DecodePoint> ToQuadFromAny(ResultPoint[] pts, float padding = 0f)
            {
                if (pts == null || pts.Length == 0)
                    return new List<DecodePoint>();

                float minX = pts[0].X, maxX = pts[0].X;
                float minY = pts[0].Y, maxY = pts[0].Y;

                for (int i = 1; i < pts.Length; i++)
                {
                    if (pts[i].X < minX) minX = pts[i].X;
                    if (pts[i].X > maxX) maxX = pts[i].X;
                    if (pts[i].Y < minY) minY = pts[i].Y;
                    if (pts[i].Y > maxY) maxY = pts[i].Y;
                }

                // 加 padding
                minX -= padding;
                maxX += padding;
                minY -= padding;
                maxY += padding;

                var topLeft = new DecodePoint { X = minX, Y = minY };
                var topRight = new DecodePoint { X = maxX, Y = minY };
                var bottomRight = new DecodePoint { X = maxX, Y = maxY };
                var bottomLeft = new DecodePoint { X = minX, Y = maxY };

                return new List<DecodePoint> { topLeft, topRight, bottomRight, bottomLeft };
            }
        }
    }
}
