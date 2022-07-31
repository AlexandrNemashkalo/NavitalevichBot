using InstagramApiSharp.Classes;
using NavitalevichBot.Models;

namespace NavitalevichBot.Exceptions;

public class InstException : Exception
{
    public readonly InstExceptionCode InstCode;

    public InstException(string message = null, ResponseType? code = null) : base(message) 
    {
        InstCode = code == null 
            ? InstExceptionCode.Unknown
            : GetCode(code.Value);
    }

    public InstException(string message, InstExceptionCode code) : base(message)
    {
        InstCode = code;
    }

    private InstExceptionCode GetCode(ResponseType exceptionText)
    {
        if (exceptionText == ResponseType.LoginRequired)
        {
            return InstExceptionCode.LoginRequired;
        }
        return InstExceptionCode.Unknown;
    }
}
