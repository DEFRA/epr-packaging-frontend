﻿using FrontendSchemeRegistration.Application.Enums;
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class NationExtensions
    {
        public static string GetNationName(string nationcode) => nationcode switch
        {
            "GB-ENG" => Nation.England.ToString(),
            "GB-SCT" => Nation.Scotland.ToString(),
            "GB-NIR" => Nation.NorthernIreland.ToString(),
            "GB-WLS" => Nation.Wales.ToString(),
            _ => ""
        };

		public static string GetNationNameFromId(int nationId) => nationId switch
		{
			(int)Nation.England => "GB-ENG",
			(int)Nation.Scotland => "GB-SCT",
			(int)Nation.Wales => "GB-WLS",
			(int)Nation.NorthernIreland => "GB-NIR",
			_ => ""
		};
	}
}
