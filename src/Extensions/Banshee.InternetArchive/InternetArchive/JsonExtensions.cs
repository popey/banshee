//
// JsonExtensions.cs
//
// Author:
//       Gabriel Burt <gabriel.burt@gmail.com>
//
// Copyright (c) 2009 Gabriel Burt
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;
using System.Globalization;
using System.Net;
using System.Linq;
using System.Collections.Generic;

using Hyena.Json;

namespace InternetArchive
{
    public static class JsonExtensions
    {
        public static T Get<T> (this JsonObject item, string key)
        {
            if (item == null)
                return default (T);

            object result;
            if (item.TryGetValue (key, out result)) {
                try {
                    if (result is T) {
                        result = (T)result;
                    } else if (result is string) {
                        var type = typeof (T);
                        string i = result as string;
                        if (type == typeof(bool)) {
                            result = Boolean.Parse (i);
                        } else if (type == typeof(Int32)) {
                            result = Int32.Parse (i);
                        } else if (type == typeof(Int64)) {
                            result = Int64.Parse (i);
                        } else if (type == typeof(double)) {
                            result = Double.Parse (i, CultureInfo.InvariantCulture);
                        } else if (type == typeof(TimeSpan)) {
                            double seconds = 0;
                            foreach (string part in i.Split (':')) {
                                seconds = seconds * 60 + Double.Parse (part, CultureInfo.InvariantCulture);
                            }
                            result = TimeSpan.FromSeconds (seconds);
                        }
                    } else if (typeof (T) == typeof (string)) {
                        result = Convert.ToString (result, CultureInfo.InvariantCulture);
                    } else if (typeof (T) == typeof (TimeSpan) && result is IConvertible) {
                        result = TimeSpan.FromSeconds (Convert.ToDouble (result, CultureInfo.InvariantCulture));
                    } else if (typeof (T) == typeof (long) || typeof (T) == typeof (int) || typeof (T) == typeof (double)) {
                        result = Convert.ChangeType (result, typeof (T), CultureInfo.InvariantCulture);
                    } else {
                        result = default (T);
                    }

                    return (T) result;
                } catch {
                    Console.WriteLine ("Couldn't cast {0} ({1}) as {2} for key {3}", result, result == null ? null : result.GetType (), typeof(T), key);
                }
            }

            return default (T);
        }

        public static string GetJoined (this JsonObject item, string key, string with)
        {
            if (item == null)
                return null;

            object value;
            if (!item.TryGetValue (key, out value) || value == null) return null;
            if (value is string) return (string)value;
            if (!(value is System.Collections.IEnumerable)) return Convert.ToString (value, CultureInfo.InvariantCulture);
            var ary = value as System.Collections.IEnumerable;
            if (ary != null) {
                return String.Join (with, ary.Cast<object> ().Select (o => o.ToString ()).ToArray ());
            }

            return null;
        }
    }
}
