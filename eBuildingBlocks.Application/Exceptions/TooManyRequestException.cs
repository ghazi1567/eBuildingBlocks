﻿namespace eBuildingBlocks.Application.Exceptions;

public class TooManyRequestException(string error) : Exception
{
    public string Error { get; } = error;
}