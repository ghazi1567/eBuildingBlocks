using System.Net;

namespace eBuildingBlocks.Application.Features;
public class ResponseModel<T> : ResponseModel
{
    public T? Data { get; set; }

    public ResponseModel<T> AddData(T value)
    {
        Data = value;
        return this;
    }

    /// <summary>
    /// Add success message and set response data
    /// </summary>
    /// <param name="value">data to be return</param>
    /// <param name="message"></param>
    /// <returns></returns>
    public ResponseModel<T> AddSuccessMessage(T value, string message)
    {
        Data = value;
        AddSuccessMessage(message);
        Success = true;
        return this;
    }

    /// <summary>
    /// add multiple success messages and set response data
    /// </summary>
    /// <param name="value"></param>
    /// <param name="messages"></param>
    /// <returns></returns>
    public ResponseModel<T> AddSuccessMessage(T value, List<string> messages)
    {
        Data = value;
        foreach (string message in messages)
        {
            AddSuccessMessage(message);
        }

        return this;
    }
}

public class ResponseModel
{


    public HttpStatusCode HttpStatusCode { get; set; }
    public bool Success { get; set; }

    public List<string> Errors { get; set; } = [];

    public Dictionary<string, string[]> ValidationErrors { get; set; } = [];

    public List<string> Successes { get; set; } = [];



    public ResponseModel AddSuccessMessage(List<string> messages)
    {
        foreach (string message in messages)
        {
            AddSuccessMessage(message);
            HttpStatusCode = HttpStatusCode.OK;
        }

        return this;
    }


    public ResponseModel AddErrorMessage(List<string> messages)
    {
        foreach (string message in messages)
        {
            AddErrorMessage(message);
            HttpStatusCode = HttpStatusCode.BadRequest;
        }
        Success = false;
        return this;
    }




    public ResponseModel AddSuccessMessage(string message)
    {
        if (!string.IsNullOrEmpty(message) && !Successes.Contains(message))
        {
            Successes.Add(message);
            HttpStatusCode = HttpStatusCode.OK;
        }
        return this;
    }



    public ResponseModel AddErrorMessage(string message)
    {
        if (!string.IsNullOrEmpty(message) && !Errors.Contains(message))
        {
            Errors.Add(message);
            HttpStatusCode = HttpStatusCode.BadRequest;
        }
        return this;
    }

    public ResponseModel AddValidationErrorMessages(Dictionary<string, string[]> errors)
    {
        ValidationErrors = errors;
        HttpStatusCode = HttpStatusCode.BadRequest;
        return this;
    }

    private ResponseModel Succeed(string message)
    {
        AddSuccessMessage(message);
        Success = true;
        HttpStatusCode = HttpStatusCode.OK;
        return this;
    }

    private ResponseModel Failed(string message)
    {
        AddErrorMessage(message);
        Success = false;
        HttpStatusCode = HttpStatusCode.BadRequest;
        return this;
    }

    private ResponseModel ValidationFailed(string message, Dictionary<string, string[]> errors)
    {
        Success = false;
        Failed(message);
        AddValidationErrorMessages(errors);
        HttpStatusCode = HttpStatusCode.BadRequest;
        return this;
    }
}
