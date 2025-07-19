namespace eBuildingBlocks.Application.Features;

public class ResponseModel<T> 
{
    public T? Data { get; private set; }

    public bool Success { get; set; }

    public List<string> Errors { get; set; } = [];

    public Dictionary<string, string[]> ValidationErrors { get; set; } = [];

    public List<string> Successes { get; set; } = [];


    public ResponseModel<T> AddData(T value)
    {
        Data = value;
        return this;
    }

    public ResponseModel<T> AddSuccessMessage(T value, string message)
    {
        Data = value;
        AddSuccessMessage(message);
        return this;
    }

    public ResponseModel<T> AddSuccessMessage(T value, List<string> messages)
    {
        Data = value;
        foreach (string message in messages)
        {
            AddSuccessMessage(message);
        }

        return this;
    }

    
    public ResponseModel<T> AddSuccessMessage(List<string> messages)
    {
        foreach (string message in messages)
        {
            AddSuccessMessage(message);
        }

        return this;
    }


    public ResponseModel<T> AddErrorMessage(List<string> messages)
    {
        foreach (string message in messages)
        {
            AddErrorMessage(message);
        }

        return this;
    }




    public ResponseModel<T> AddSuccessMessage(string message)
    {
        if (!string.IsNullOrEmpty(message) && !Successes.Contains(message))
        {
            Successes.Add(message);
        }
        return this;
    }
    

  
    public ResponseModel<T> AddErrorMessage(string message)
    {
        if (!string.IsNullOrEmpty(message) && !Errors.Contains(message))
        {
            Errors.Add(message);
        }
        return this;
    }
  
    public ResponseModel<T>  AddValidationErrorMessages(Dictionary<string, string[]> errors)
    {
        ValidationErrors = errors;
        return this;
    }
  
    private ResponseModel<T>  Succeed(string message)
    {
        AddSuccessMessage(message);
        Success = true;
        return this;
    }

    private ResponseModel<T>  Failed(string message)
    {
        AddErrorMessage(message);
        Success = false;
        return this;
    }

    private ResponseModel<T>  ValidationFailed(string message, Dictionary<string, string[]> errors)
    {
        Success = false;
        Failed(message);
        AddValidationErrorMessages(errors);
        return this;
    }
}
