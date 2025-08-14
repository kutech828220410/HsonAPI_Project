using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HsonAPILib
{
    /// <summary>
    /// 請求資料：包含 Base64 圖片字串
    /// </summary>
    public class DecodeRequest
    {
        /// <summary>
        /// 圖片的 Base64 字串（可含 data URL 前綴，例如 data:image/png;base64,）
        /// </summary>
        public string ImageBase64 { get; set; } = "";
    }

    /// <summary>
    /// 條碼結果的座標點
    /// </summary>
    public class DecodePoint
    {
        /// <summary>
        /// X 座標（像素單位）
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Y 座標（像素單位）
        /// </summary>
        public float Y { get; set; }
    }

    /// <summary>
    /// 條碼解碼結果資料
    /// </summary>
    public class DecodeResultDto
    {
        /// <summary>
        /// 條碼內容文字
        /// </summary>
        public string Text { get; set; } = "";

        /// <summary>
        /// 條碼格式（例如：QR_CODE、CODE_128）
        /// </summary>
        public string Format { get; set; } = "";

        /// <summary>
        /// 是否為二維條碼
        /// </summary>
        public bool Is2D { get; set; }

        /// <summary>
        /// 條碼區域的頂點座標集合
        /// </summary>
        public List<DecodePoint> Points { get; set; } = new List<DecodePoint>();
    }

    /// <summary>
    /// 條碼解碼 API 回應資料
    /// </summary>
    public class DecodeResponse
    {
        /// <summary>
        /// 偵測到的條碼數量
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 條碼解碼結果清單
        /// </summary>
        public List<DecodeResultDto> Results { get; set; } = new List<DecodeResultDto>();

        /// <summary>
        /// 訊息（成功、錯誤或其他狀態）
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 處理耗時（毫秒）
        /// </summary>
        public long ElapsedMs { get; set; }
    }

}
