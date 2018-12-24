using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HelpersForCore
{
    public class ResponseBase<T>
    {
        public int Code { get; set; }
        public T Result { get; set; }
        public string Error { get; set; }
        public string Description { get; set; }

        public bool HasError => string.IsNullOrWhiteSpace(Error) == false;
        public ObjectResult FailedResult => new ObjectResult(Error)
        {
            StatusCode = Code
        };

        public ResponseBase() { }
        public ResponseBase(T result)
        {
            Result = result;
        }

        public void WithError(int code, string error, string description = null)
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                error = "可預期但沒有描述的錯誤。(應在程式開發時加以描述)";
            }
            Code = code;
            Error = error;
            Description = description;
        }
    }

    public class MultiResponse<T> : ResponseBase<IEnumerable<T>>
    {
        public bool IsPaged { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int Count { get; set; }

        public MultiResponse() { }
        public MultiResponse(IEnumerable<T> result)
        {
            Result = result;
            Count = Result.Count();
        }
        public MultiResponse(IEnumerable<T> result, int pageNumber, int pageSize)
        {
            Result = result;
            Page(pageNumber, pageSize);
        }
        public void Page(int pageNumber, int pageSize)
        {
            IsPaged = true;
            PageNumber = pageNumber;
            PageSize = pageSize;
            Count = Result.Count();
            Result = Result.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        }
    }
}
