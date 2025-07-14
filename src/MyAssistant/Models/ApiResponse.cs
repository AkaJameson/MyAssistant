namespace MyAssistant.Models
{
    public class ApiResponse<T>
    {
        public int Code { get; set; }        // 状态码，例如 200、400、500
        public string Message { get; set; }  // 提示信息
        public T Data { get; set; }          // 返回的数据内容

        public static ApiResponse<T> Success(T data, string message = "OK")
        {
            return new ApiResponse<T> { Code = 200, Message = message, Data = data };
        }

        public static ApiResponse<T> Fail(string message, int code = 500)
        {
            return new ApiResponse<T> { Code = code, Message = message, Data = default };
        }
    }

    public class ApiResponse
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public static ApiResponse Success(string message = "OK")
        {
            return new ApiResponse { Code = 200, Message = message };
        }
        public static ApiResponse Fail(string message, int code = 500)
        {
            return new ApiResponse { Code = code, Message = message };
        }

    }
}
