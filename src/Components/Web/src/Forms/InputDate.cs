// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// An input component for editing date values.
    /// Supported types are <see cref="DateTime"/> and <see cref="DateTimeOffset"/>.
    /// </summary>
    public class InputDate<TValue> : InputBase<TValue>
    {
        private const string DateFormat = "yyyy-MM-dd"; // Compatible with HTML date inputs
        private const string MonthFormat = "yyyy-MM"; // Compatible with HTML month inputs
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss"; // Compatible with HTML date time inputs
        private const string TimeFormat = "HH:mm:ss"; // Compatible with HTML time inputs

        private string HtmlValueFormat => DateInputType switch
        {
            DateInputType.Time => TimeFormat,
            DateInputType.Month => MonthFormat,
            _ => DateFormat,
        };

        // Gets the format used propery Parse from HTML value to .NET DateTime.
        private string DotnetValueFormat => DateInputType == DateInputType.Time ? DateTimeFormat : DateFormat;

        // The HTML input element has multiple formats depending on it's type.
        // For some types there no direct mapping to the .NET DateTime so padding must be provided with default values.
        private string MapToDotnetValue(string? value)
        {
            return DateInputType switch
            {
                DateInputType.Time => $"0001-01-01 {value:TimeFormat}",
                DateInputType.Month => $"{value:MonthFormat}-01",
                _ => $"{value:DateTimeFormat}"
            };
        }

        /// <summary>
        /// Gets or sets the error message used when displaying an a parsing error.
        /// </summary>
        [Parameter] public string ParsingErrorMessage { get; set; } = "The {0} field must be a date.";

        /// <summary>
        /// Gets or sets the HTML input type. Determines the input fields show to the user.
        /// </summary>
        [Parameter] public DateInputType DateInputType { get; set; } = DateInputType.Date;

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "input");
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "type", DateInputType.ToString().ToLower());
            builder.AddAttribute(3, "class", CssClass);
            builder.AddAttribute(4, "value", BindConverter.FormatValue(CurrentValueAsString));
            builder.AddAttribute(5, "onchange", EventCallback.Factory.CreateBinder<string?>(this, __value => CurrentValueAsString = __value, CurrentValueAsString));
            builder.CloseElement();
        }

        /// <inheritdoc />
        protected override string FormatValueAsString([AllowNull] TValue value)
        {
            switch (value)
            {
                case DateTime dateTimeValue:
                    return BindConverter.FormatValue(dateTimeValue, HtmlValueFormat, CultureInfo.InvariantCulture);
                case DateTimeOffset dateTimeOffsetValue:
                    return BindConverter.FormatValue(dateTimeOffsetValue, HtmlValueFormat, CultureInfo.InvariantCulture);
                default:
                    return string.Empty; // Handles null for Nullable<DateTime>, etc.
            }
        }

        /// <inheritdoc />
        protected override bool TryParseValueFromString(string? value, [MaybeNull] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
        {
            // Unwrap nullable types. We don't have to deal with receiving empty values for nullable
            // types here, because the underlying InputBase already covers that.
            var targetType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);

            bool success;
            if (targetType == typeof(DateTime))
            {
                success = TryParseDateTime(MapToDotnetValue(value), out result);
            }
            else if (targetType == typeof(DateTimeOffset))
            {
                success = TryParseDateTimeOffset(MapToDotnetValue(value), out result);
            }
            else
            {
                throw new InvalidOperationException($"The type '{targetType}' is not a supported date type.");
            }

            if (success)
            {
                validationErrorMessage = null;
                return true;
            }
            else
            {
                validationErrorMessage = string.Format(ParsingErrorMessage, FieldIdentifier.FieldName);
                return false;
            }
        }

        private bool TryParseDateTime(string? value, [MaybeNullWhen(false)] out TValue result)
        {


            var success = BindConverter.TryConvertToDateTime(value, CultureInfo.InvariantCulture, DotnetValueFormat, out var parsedValue);
            if (success)
            {
                result = (TValue)(object)parsedValue;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        private bool TryParseDateTimeOffset(string? value, [MaybeNullWhen(false)] out TValue result)
        {

            var success = BindConverter.TryConvertToDateTimeOffset(value, CultureInfo.InvariantCulture, DotnetValueFormat, out var parsedValue);
            if (success)
            {
                result = (TValue)(object)parsedValue;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }
    }
}
