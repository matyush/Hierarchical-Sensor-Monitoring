﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HSMCommon.Constants;
using HSMSensorDataObjects;
using HSMSensorDataObjects.SensorValueRequests;
using HSMServer.BackgroundServices;
using HSMServer.Core.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HSMServer.Middleware;

public class PermissionFilter(IPermissionService service, DataCollectorWrapper collector) : IAsyncActionFilter, IAsyncResultFilter
{
    private const string ErrorTooLongPath = "Path for the sensor is too long.";
    private const string ErrorInvalidPath = "Path has an invalid format.";
    private const string ErrorPathKey = "Path or key is empty.";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var headers = context.HttpContext.Request.Headers;
        var hasKey = GetKeyIdFromHeader(headers, out var keyId);

        if (!hasKey)
            return;

        var keyExist = service.TryGetKey(keyId, out var key, out var message);

        if (!keyExist)
            return;

        var productExist = service.TryGetProduct(key.ProductId, out var product, out message);

        if (!productExist)
            return;
        
        var values = context.ActionArguments.Values.FirstOrDefault();// objects from argumaents: sensor list / value;
        var collectorName = context.HttpContext.Request.Headers.TryGetValue(nameof(Header.ClientName), out var clientName) && !string.IsNullOrWhiteSpace(clientName) ? clientName.ToString() : "No name";
        var path = $"{product.DisplayName}/{key.DisplayName}/{collectorName}";
        context.HttpContext.Request.Headers["Path"] = path;
        
        collector.Statistics[path].AddRequestData(context.HttpContext.Request);
        collector.Statistics.Total.AddRequestData(context.HttpContext.Request);
        
        if (values is BaseRequest request && !service.CheckWritePermissions(product, key, GetPathParts(request?.Path), out message))
        {
            context.HttpContext.Response.StatusCode = 406;
            return;
        }
        

        if (values is List<SensorValueBase> requestValues)
        {
            context.ActionArguments.Remove("values");
          
            requestValues = requestValues.Where(x => service.CheckWritePermissions(product, key, GetPathParts(x?.Path), out message)).ToList();
            
            context.ActionArguments.Add("values", requestValues);
            
            collector.Statistics.Total.AddReceiveData(requestValues.Count);
            collector.Statistics[context.HttpContext.Request.Headers["Path"]].AddReceiveData(requestValues.Count);
        }
        else
        {
            collector.Statistics.Total.AddReceiveData(1);
            collector.Statistics[context.HttpContext.Request.Headers["Path"]].AddReceiveData(1);
        }

        await next();
    }
    
    
    private static bool GetKeyIdFromHeader(BaseRequest request, IHeaderDictionary headers, out Guid guidKey)
    {
        headers.TryGetValue(nameof(Header.Key), out var key);

        if (string.IsNullOrEmpty(key))
            key = request?.Key;

        return Guid.TryParse(key, out guidKey);
    }

    private static bool GetKeyIdFromHeader(IHeaderDictionary headers, out Guid guidKey)
    {
        headers.TryGetValue(nameof(Header.Key), out var key);

        return Guid.TryParse(key, out guidKey);
    }

    private static ReadOnlySpan<string> GetPathParts(string path)
    {
        path = path.FirstOrDefault() == CommonConstants.SensorPathSeparator ? path[1..] : path;

        return path.Split(CommonConstants.SensorPathSeparator, StringSplitOptions.TrimEntries).AsSpan();
    }
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        collector.Statistics[context.HttpContext.Request.Headers["Path"]].AddResponseResult(context.HttpContext.Response);
        await next();
    }
}
