﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Omex.Extensions.Abstractions;

namespace Microsoft.Omex.Extensions.Diagnostics.HealthChecks
{
	/// <summary>
	/// Extension to add health checks into IHealthChecksBuilder
	/// </summary>
	public static class HealthChecksBuilderExtensions
	{
		/// <summary>
		/// Add http endpoint health check
		/// </summary>
		/// <param name="builder">health checks builder</param>
		/// <param name="name">name of the health check</param>
		/// <param name="endpointName">name of the endpoint to check</param>
		/// <param name="relativePath">relative path to check, absolute path not allowed</param>
		/// <param name="method">http method to use, defaults to HttpGet</param>
		/// <param name="scheme">uri scheme, defaults to http</param>
		/// <param name="headers">headers to attach to the request</param>
		/// <param name="expectedStatus">response status code that considered healthy, default to 200(OK)</param>
		/// <param name="additionalCheck">action that would be called after getting response, function should return new result object that would be reported</param>
		/// <param name="reportData">additional properties that will be attached to health check result, for example escalation info</param>
		public static IHealthChecksBuilder AddHttpEndpointCheck(
			this IHealthChecksBuilder builder,
			string name,
			string endpointName,
			string relativePath,
			HttpMethod? method = null,
			string? scheme = null,
			IReadOnlyDictionary<string, IEnumerable<string>>? headers = null,
			HttpStatusCode? expectedStatus = null,
			Func<HttpResponseMessage, HealthCheckResult, HealthCheckResult>? additionalCheck = null,
			params KeyValuePair<string, object>[] reportData)
		{
			Func<UriBuilder, HttpRequestMessage> httpRequest = uriBuilder =>
			{
				uriBuilder.Path = relativePath;

				uriBuilder.Scheme = scheme == null
					? Uri.UriSchemeHttp
					: Uri.CheckSchemeName(scheme)
						? scheme
						: throw new ArgumentException("Invalid uri scheme", nameof(scheme));

				HttpRequestMessage request = new HttpRequestMessage(method ?? HttpMethod.Get, uriBuilder.Uri);

				if (headers != null)
				{
					foreach (KeyValuePair<string, IEnumerable<string>> pair in headers)
					{
						request.Headers.TryAddWithoutValidation(pair.Key, pair.Value);
					}
				}

				return request;
			};

			return builder.AddHttpEndpointCheck(name, endpointName, httpRequest, expectedStatus, additionalCheck, reportData);
		}

		/// <summary>
		/// Add http endpoint health check
		/// </summary>
		/// <param name="builder">health checks builder</param>
		/// <param name="name">name of the health check</param>
		/// <param name="endpointName">name of the endpoint to check</param>
		/// <param name="httpRequestMessageBuilder">action that will return the http request message</param>
		/// <param name="expectedStatus">response status code that considered healthy, default to 200(OK)</param>
		/// <param name="additionalCheck">action that would be called after getting response, function should return new result object that would be reported</param>
		/// <param name="reportData">additional properties that will be attached to health check result, for example escalation info</param>
		public static IHealthChecksBuilder AddHttpEndpointCheck(
			this IHealthChecksBuilder builder,
			string name,
			string endpointName,
			Func<UriBuilder, HttpRequestMessage> httpRequestMessageBuilder,
			HttpStatusCode? expectedStatus = null,
			Func<HttpResponseMessage, HealthCheckResult, HealthCheckResult>? additionalCheck = null,
			params KeyValuePair<string, object>[] reportData)
		{
			int port = SfConfigurationProvider.GetEndpointPort(endpointName);
			UriBuilder uriBuilder = new UriBuilder(Uri.UriSchemeHttp, "localhost", port);
			HttpRequestMessage request = httpRequestMessageBuilder(uriBuilder);

			return builder.AddTypeActivatedCheck<HttpEndpointHealthCheck>(
				name,
				new HttpHealthCheckParameters(request, expectedStatus, additionalCheck, reportData));
		}
	}
}
