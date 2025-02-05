﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Models.Common;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Features.Export;

internal sealed class IdentifierExportSource : IExportSource
{
    public event EventHandler<ReadFailureEventArgs> ReadFailure;

    public ExportDataOptions<ExportSourceType> Description => _identifiers.Count > 0 ? CreateOptions(_identifiers) : null;

    private readonly IInstanceStore _instanceStore;
    private readonly PartitionEntry _partition;
    private readonly Queue<DicomIdentifier> _identifiers;

    public IdentifierExportSource(IInstanceStore instanceStore, PartitionEntry partition, IOptions<IdentifierExportOptions> options)
    {
        _instanceStore = EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
        _partition = EnsureArg.IsNotNull(partition, nameof(partition));
        _identifiers = new Queue<DicomIdentifier>(EnsureArg.IsNotNull(options?.Value.Values, nameof(options)));
    }

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    public async IAsyncEnumerator<ReadResult> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        IEnumerable<ReadResult> results = Enumerable.Empty<ReadResult>();
        foreach (DicomIdentifier identifier in _identifiers)
        {
            // Attempt to read the data
            IReadOnlyList<VersionedInstanceIdentifier> instances = identifier.Type switch
            {
                ResourceType.Study => await _instanceStore.GetInstanceIdentifiersInStudyAsync(_partition.PartitionKey, identifier.StudyInstanceUid, cancellationToken),
                ResourceType.Series => await _instanceStore.GetInstanceIdentifiersInSeriesAsync(_partition.PartitionKey, identifier.StudyInstanceUid, identifier.SeriesInstanceUid, cancellationToken),
                _ => await _instanceStore.GetInstanceIdentifierAsync(_partition.PartitionKey, identifier.StudyInstanceUid, identifier.SeriesInstanceUid, identifier.SopInstanceUid, cancellationToken),
            };

            if (instances.Count == 0)
            {
                var args = new ReadFailureEventArgs(
                    identifier,
                    identifier.Type switch
                    {
                        ResourceType.Study => new StudyNotFoundException(),
                        ResourceType.Series => new SeriesNotFoundException(),
                        _ => new InstanceNotFoundException(),
                    });

                ReadFailure?.Invoke(this, args);
                yield return ReadResult.ForFailure(args);
            }
            else
            {
                foreach (VersionedInstanceIdentifier read in instances)
                    yield return ReadResult.ForIdentifier(read);
            }
        }
    }

    public bool TryDequeueBatch(int size, out ExportDataOptions<ExportSourceType> batch)
    {
        EnsureArg.IsGt(size, 0, nameof(size));

        if (_identifiers.Count == 0)
        {
            batch = default;
            return false;
        }

        var elements = new List<DicomIdentifier>();
        while (elements.Count < size && _identifiers.TryDequeue(out DicomIdentifier identifier))
        {
            elements.Add(identifier);
        }

        batch = CreateOptions(elements);
        return true;
    }

    private static ExportDataOptions<ExportSourceType> CreateOptions(IReadOnlyCollection<DicomIdentifier> values)
        => new ExportDataOptions<ExportSourceType>(ExportSourceType.Identifiers, new IdentifierExportOptions { Values = values });
}
