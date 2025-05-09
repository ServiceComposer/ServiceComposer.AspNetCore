﻿using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;

namespace Snippets.SampleHandler
{
    // begin-snippet: sample-handler-with-authorization
    public class SampleHandlerWithAuthorization : ICompositionRequestsHandler
    {
        [Authorize]
        [HttpGet("/sample/{id}")]
        public Task Handle(HttpRequest request)
        {
            return Task.CompletedTask;
        }
    }
    // end-snippet

    // begin-snippet: sample-handler-with-custom-status-code
    public class SampleHandlerWithCustomStatusCode : ICompositionRequestsHandler
    {
        [HttpGet("/sample/{id}")]
        public Task Handle(HttpRequest request)
        {
            var response = request.HttpContext.Response;
            response.StatusCode = (int)HttpStatusCode.Forbidden;

            return Task.CompletedTask;
        }
    }
    // end-snippet
}