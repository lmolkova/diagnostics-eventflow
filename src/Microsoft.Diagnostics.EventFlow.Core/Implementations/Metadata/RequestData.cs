﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Diagnostics;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Metadata
{
    public enum DurationUnit
    {
        TimeSpan,
        Milliseconds,
        Seconds,
        Minutes,
        Hours
    };

    public class RequestData
    {
        public static readonly string RequestMetadataKind = "request";
        public static readonly string RequestNamePropertyMoniker = "requestNameProperty";
        public static readonly string IsSuccessPropertyMoniker = "isSuccessProperty";
        public static readonly string DurationPropertyMoniker = "durationProperty";
        public static readonly string DurationUnitMoniker = "durationUnit";
        public static readonly string ResponseCodePropertyMoniker = "responseCodeProperty";

        public string RequestName { get; private set; }
        public TimeSpan? Duration { get; private set; }
        public bool? IsSuccess { get; private set; }
        public string ResponseCode { get; private set; }

        // Ensure that RequestData can only be created using TryGetRequestData() method
        private RequestData() { }

        public static DataRetrievalResult TryGetData(
            EventData eventData,
            EventMetadata requestMetadata,
            out RequestData request)
        {
            Requires.NotNull(eventData, nameof(eventData));
            Requires.NotNull(requestMetadata, nameof(requestMetadata));
            request = null;

            string requestNameProperty = requestMetadata[RequestNamePropertyMoniker];
            if (string.IsNullOrWhiteSpace(requestNameProperty))
            {
                return DataRetrievalResult.MissingMetadataProperty(RequestNamePropertyMoniker);
            }

            string requestName = null;
            if (!eventData.GetValueFromPayload<string>(requestNameProperty, (v) => requestName = v))
            {
                return DataRetrievalResult.DataMissingOrInvalid(requestNameProperty);
            }

            bool? success = null;
            string isSuccessProperty = requestMetadata[IsSuccessPropertyMoniker];
            if (!string.IsNullOrWhiteSpace(isSuccessProperty))
            {
                if (!eventData.GetValueFromPayload<bool>(isSuccessProperty, (v) => success = v))
                {
                    return DataRetrievalResult.DataMissingOrInvalid(isSuccessProperty);
                }
            }

            TimeSpan? duration = null;
            string durationProperty = requestMetadata[DurationPropertyMoniker];
            if (!string.IsNullOrWhiteSpace(durationProperty))
            {
                DurationUnit durationUnit;
                string durationUnitOverride = requestMetadata[DurationUnitMoniker];
                if (string.IsNullOrEmpty(durationUnitOverride) || !Enum.TryParse<DurationUnit>(durationUnitOverride, ignoreCase: true, result: out durationUnit))
                {
                    // By default we assume duration is stored as a double value representing milliseconds
                    durationUnit = DurationUnit.Milliseconds;
                }

                if (durationUnit != DurationUnit.TimeSpan)
                {
                    double tempDuration = 0.0;
                    if (!eventData.GetValueFromPayload<double>(durationProperty, (v) => tempDuration = v))
                    {
                        return DataRetrievalResult.DataMissingOrInvalid(durationProperty);
                    }
                    duration = ToTimeSpan(tempDuration, durationUnit);
                }
                else
                {
                    if (!eventData.GetValueFromPayload<TimeSpan>(durationProperty, (v) => duration = v))
                    {
                        return DataRetrievalResult.DataMissingOrInvalid(durationProperty);
                    }
                }
            }

            string responseCode = null;
            string responseCodeProperty = requestMetadata[ResponseCodePropertyMoniker];
            if (!string.IsNullOrWhiteSpace(responseCodeProperty))
            {
                if (!eventData.GetValueFromPayload<string>(responseCodeProperty, (v) => responseCode = v))
                {
                    return DataRetrievalResult.DataMissingOrInvalid(responseCodeProperty);
                }
            }

            request = new RequestData();
            request.RequestName = requestName;
            request.IsSuccess = success;
            request.Duration = duration;
            request.ResponseCode = responseCode;
            return DataRetrievalResult.Success();
        }

        private static TimeSpan ToTimeSpan(double value, DurationUnit durationUnit)
        {
            switch (durationUnit)
            {
                case DurationUnit.Milliseconds:
                    return TimeSpan.FromMilliseconds(value);
                case DurationUnit.Seconds:
                    return TimeSpan.FromSeconds(value);
                case DurationUnit.Minutes:
                    return TimeSpan.FromMinutes(value);
                case DurationUnit.Hours:
                    return TimeSpan.FromHours(value);
                default:
                    throw new ArgumentOutOfRangeException(nameof(durationUnit), "Error during request data extraction: unexpected durationUnit value");
            }
        }
    }
}
