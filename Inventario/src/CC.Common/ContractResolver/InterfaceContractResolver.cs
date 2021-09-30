﻿using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace Common.Helpers.Contract
{
    public class InterfaceContractResolver : DefaultContractResolver
    {
        private readonly Type _InterfaceType;
        public InterfaceContractResolver(Type InterfaceType)
        {
            _InterfaceType = InterfaceType;
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> properties = base.CreateProperties(_InterfaceType, memberSerialization);

            return properties;
        }
    }
}
