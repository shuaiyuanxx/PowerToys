﻿#pragma warning disable IDE0073
// Copyright (c) Brice Lambson
// The Brice Lambson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.  Code forked from Brice Lambson's https://github.com/bricelam/ImageResizer/
#pragma warning restore IDE0073

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ImageResizer.Views
{
    [ValueConversion(typeof(Enum), typeof(int))]
    internal class EnumToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (int)value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => targetType.GetEnumValues().GetValue((int)value);
    }
}
