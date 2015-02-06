﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Reads an object from the request body.
    /// </summary>
    public abstract class InputFormatter : IInputFormatter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputFormatter"/> class.
        /// </summary>
        protected InputFormatter()
        {
            SupportedEncodings = new List<Encoding>();
            SupportedMediaTypes = new List<MediaTypeHeaderValue>();
        }

        /// <summary>
        /// Gets the mutable collection of character encodings supported by
        /// this <see cref="InputFormatter"/>. The encodings are
        /// used when writing the data.
        /// </summary>
        public IList<Encoding> SupportedEncodings { get; }

        /// <summary>
        /// Gets the mutable collection of <see cref="MediaTypeHeaderValue"/> elements supported by
        /// this <see cref="InputFormatter"/>.
        /// </summary>
        public IList<MediaTypeHeaderValue> SupportedMediaTypes { get; }

        protected object GetDefaultValueForType(Type modelType)
        {
            if (modelType.GetTypeInfo().IsValueType)
            {
                return Activator.CreateInstance(modelType);
            }

            return null;
        }

        /// <inheritdoc />
        public virtual bool CanRead(InputFormatterContext context)
        {
            var contentType = context.ActionContext.HttpContext.Request.ContentType;
            MediaTypeHeaderValue requestContentType;
            if (!MediaTypeHeaderValue.TryParse(contentType, out requestContentType))
            {
                return false;
            }

            return SupportedMediaTypes
                            .Any(supportedMediaType => supportedMediaType.IsSubsetOf(requestContentType));
        }

        /// <inheritdoc />
        public virtual async Task<object> ReadAsync(InputFormatterContext context)
        {
            var request = context.ActionContext.HttpContext.Request;
            if (request.ContentLength == 0)
            {
                return GetDefaultValueForType(context.ModelType);
            }

            return await ReadRequestBodyAsync(context);
        }

        /// <summary>
        /// Reads the request body.
        /// </summary>
        /// <param name="context">The <see cref="InputFormatterContext"/> associated with the call.</param>
        /// <returns>A task which can read the request body.</returns>
        public abstract Task<object> ReadRequestBodyAsync(InputFormatterContext context);

        /// <summary>
        /// Returns encoding based on content type charset parameter.
        /// </summary>
        protected Encoding SelectCharacterEncoding(MediaTypeHeaderValue contentType)
        {
            if (contentType != null)
            {
                var charset = contentType.Charset;
                if (!string.IsNullOrWhiteSpace(contentType.Charset))
                {
                    foreach (var supportedEncoding in SupportedEncodings)
                    {
                        if (string.Equals(charset, supportedEncoding.WebName, StringComparison.OrdinalIgnoreCase))
                        {
                            return supportedEncoding;
                        }
                    }
                }
            }

            if (SupportedEncodings.Count > 0)
            {
                return SupportedEncodings[0];
            }

            // No supported encoding was found so there is no way for us to start reading.
            throw new InvalidOperationException(Resources.FormatInputFormatterNoEncoding(GetType().FullName));
        }
    }
}