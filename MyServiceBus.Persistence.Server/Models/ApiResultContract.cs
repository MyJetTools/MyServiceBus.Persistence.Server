namespace MyServiceBus.Persistence.Server.Models
{

    public enum ApiResult
    {
        Ok, RecordNotFound
    }
    
    public class ApiResultContract<TDataType>
    {
        public ApiResult Result { get; set; }
        
        public TDataType Data { get; set; }

        public static ApiResultContract<TDataType> CreateOk(TDataType data)
        {
            return new()
            {
                Result = ApiResult.Ok,
                Data = data
            };
        }

        public static ApiResultContract<TDataType> CreateFail(ApiResult apiResult)
        {
            return new()
            {
                Result = apiResult
            };
        }
    }
}