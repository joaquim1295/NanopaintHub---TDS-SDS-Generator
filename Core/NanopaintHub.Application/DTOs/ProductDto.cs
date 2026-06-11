namespace NanopaintHub.Application.DTOs;

using System;
using System.Collections.Generic;

public record ProductDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string CategoryName,
    List<string> Features,
    Dictionary<string,string> LiquidProperties,
    Dictionary<string,string> MechanicalProperties,
    Dictionary<string, string>? FunctionalProperties
    );