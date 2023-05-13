namespace SkripsiAppBackend.Common.Exceptions
{
    public static class ExceptionHelpers
    {
        public struct ResultOrException<T>
        {
            public T? Value { get; set; }
            public Exception? Exception { get; set; }
        }

        public static async Task<ResultOrException<T>> GetResultOrException<T>(Func<Task<T>> GetResult)
        {
            try
            {
                var result = await GetResult();
                return new ResultOrException<T>() { Value = result };
            }
            catch (Exception e)
            {
                return new ResultOrException<T>() { Exception = e };
            }
        }

        public static ResultOrException<T> GetResultOrException<T>(Func<T> GetResult)
        {
            try
            {
                var result = GetResult();

                return new ResultOrException<T>() { Value = result };
            }
            catch (Exception e)
            {
                return new ResultOrException<T>() { Exception = e };
            }
        }

        public static List<string> GetErrors(params Exception[] resultOrExceptions)
        {
            return GetErrors(resultOrExceptions.ToList());
        }

        public static List<string> GetErrors(List<Exception> exceptions)
        {
            return exceptions
                .Where(e => e != null)
                .Select(e => e.Message)
                .ToList();
        }
    }
}
