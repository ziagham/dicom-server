﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Features.Export;

internal abstract class ExportSourceProvider<TOptions> : IExportSourceProvider
{
    public abstract ExportSourceType Type { get; }

    public Task<IExportSource> CreateAsync(IServiceProvider provider, object options, PartitionEntry partition, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(provider, nameof(provider));
        EnsureArg.IsNotNull(options, nameof(options));
        EnsureArg.IsNotNull(partition, nameof(partition));
        return CreateAsync(provider, (TOptions)options, partition, cancellationToken);
    }

    public Task ValidateAsync(object options, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(options, nameof(options));
        return ValidateAsync((TOptions)options, cancellationToken);
    }

    protected abstract Task<IExportSource> CreateAsync(IServiceProvider provider, TOptions options, PartitionEntry partition, CancellationToken cancellationToken = default);

    protected abstract Task ValidateAsync(TOptions options, CancellationToken cancellationToken = default);
}
