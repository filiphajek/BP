﻿namespace TaskLauncher.Api.Contracts.Requests;

public class AddOrUpdateConfigValueRequest
{
    public string Key { get; set; }
    public string Value { get; set; }
    public string Description { get; set; }
}
