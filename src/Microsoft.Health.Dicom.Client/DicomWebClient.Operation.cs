﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Client;

public partial class DicomWebClient : IDicomWebClient
{
    public async Task<DicomWebResponse<OperationState<DicomOperation>>> GetOperationStateAsync(Guid operationId, CancellationToken cancellationToken)
    {
        var uri = new Uri($"/{_apiVersion}{DicomWebConstants.BaseOperationUri}/{operationId.ToString(OperationId.FormatSpecifier)}", UriKind.Relative);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);
        return new DicomWebResponse<OperationState<DicomOperation>>(response, ValueFactory<OperationState<DicomOperation>>);
    }
}
