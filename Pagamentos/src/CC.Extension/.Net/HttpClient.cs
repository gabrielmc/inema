﻿using CC.Extension.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CC.Extension.Net
{
    public static class HttpClientExtensions
    {
        #region [ Configuration ]

        /// <summary>
        /// Habilita suporte a comunicacoes com canais securos TLS / SSL
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static void EnableSupportTLS()
        {
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        /// <summary>
        /// Retorna o Handler do HTTP Cliente com suporte a conteudo com compressão
        /// </summary>
        /// <returns></returns>
        public static HttpClientHandler WithCompression()
        {
            return new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                ClientCertificateOptions = ClientCertificateOption.Automatic

            };
            
        }

        #endregion

        #region [ Header ]

        /// <summary>
        /// Limpa o Header da Requisição
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static HttpClient ClearHeader(this HttpClient client)
        {
            if (client.DefaultRequestHeaders.Count() > 0)
                client.DefaultRequestHeaders.ToList().ForEach(x =>
                {
                    client.DefaultRequestHeaders.Remove(x.Key);
                });

            return client;
        }

        /// <summary>
        /// Adiciona um Dicionario ao header do HttpClient
        /// </summary>
        /// <param name="client">HttpClient</param>
        /// <param name="header">Dicionario de Parametros a serem adicionado ao Header</param>
        /// <returns></returns>
        public static HttpClient AddHeaderCollection(this HttpClient client, IDictionary<string, string> header)
        {
            #region [ Code ]
            if (header?.Count > 0)
                foreach (var item in header)
                {
                    if (!string.IsNullOrWhiteSpace(item.Key) && !string.IsNullOrWhiteSpace(item.Value) && !client.DefaultRequestHeaders.Any(x => x.Key == item.Key))
                        client.DefaultRequestHeaders.TryAddWithoutValidation(item.Key, item.Value);
                }
            return client;
            #endregion
        }

        /// <summary>
        /// Cria na mensagem um cabeçalho de requisição
        /// </summary>
        /// <param name="httpRequestMessage"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public static HttpRequestMessage AddHeaderCollection(this HttpRequestMessage httpRequestMessage, IDictionary<string, string> headers)
        {
            if (headers == null)
                return httpRequestMessage;

            foreach (var data in headers)
            {
                if (String.IsNullOrWhiteSpace(data.Key) == false && String.IsNullOrWhiteSpace(data.Value) == false)
                {
                    httpRequestMessage.Headers.TryAddWithoutValidation(data.Key, data.Value);
                }
            }

            return httpRequestMessage;
        }

        /// <summary>
        /// Adicona um novo item ao Header
        /// </summary>
        /// <param name="httpRequestMessage"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static HttpRequestMessage AddHeader(this HttpRequestMessage httpRequestMessage, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                return httpRequestMessage;

            return httpRequestMessage.AddHeaderCollection(new Dictionary<string, string> { { key, value } });
        }


        #endregion

        #region [ PayLoad ]

        /// <summary>
        /// Cria o PayLoad da requisição com Base em um dicionario 
        /// </summary>
        /// <param name="client"></param>
        /// <param name=""></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public static HttpRequestMessage Dictionary2Json(this HttpRequestMessage client, Dictionary<string, string> payload)
        {
            #region [ Code ]

            if (payload == null)
                return null;

            StringBuilder body = new StringBuilder("{");

            if (payload?.Count > 0)
                foreach (var item in payload)
                {
                    if (!string.IsNullOrWhiteSpace(item.Key) && !string.IsNullOrWhiteSpace(item.Value))
                        body.AppendLine($"\"{item.Key}\": \"{item.Value}\"");
                }
            body.AppendLine("}");

            client.Content = new StringContent(body.ToString());

            return client;

            #endregion
        }

        /// <summary>
        /// Convert um objeto em uma String para envio no PayLoad
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static HttpRequestMessage SerializeJson<T>(this HttpRequestMessage client, T data) => client.SerializeJson<T>(data, null, null);

        /// <summary>
        /// Convert um objeto em uma String para envio no PayLoad
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="data"></param>
        /// <param name="jsonSerializerSettings"></param>
        /// <returns></returns>
        public static HttpRequestMessage SerializeJson<T>(this HttpRequestMessage client, T data, JsonSerializerSettings jsonSerializerSettings) => client.SerializeJson<T>(data, null, jsonSerializerSettings);

        /// <summary>
        /// Convert um objeto em uma String para envio no PayLoad
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="data"></param>
        /// <param name="contractType"></param>
        /// <param name="jsonSerializerSettings"></param>
        /// <returns></returns>
        public static HttpRequestMessage SerializeJson<T>(this HttpRequestMessage client, T data, Type contractType, JsonSerializerSettings jsonSerializerSettings)
        {
            #region [ Code ]

            if (data == null)
                client.Content = new StringContent("{}", Encoding.UTF8, "application/json");
            else
                client.Content = new StringContent(
                    contractType == null && jsonSerializerSettings == null ?
                        JsonConvert.SerializeObject(data)
                    : contractType != null && jsonSerializerSettings != null ?
                        JsonConvert.SerializeObject(data, contractType, jsonSerializerSettings)
                    :
                        JsonConvert.SerializeObject(data, jsonSerializerSettings)
                        , Encoding.UTF8,
                        "application/json"
                    );

            return client;
            #endregion
        }

        /// <summary>
        /// Formata um objeto no modelo de Formulario para uso do HttpRequestMessage
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static HttpRequestMessage SerializeForm<T>(this HttpRequestMessage client, T data)
        {
            #region [ Code ]

            if (data == null)
                return null;

            var result = new List<KeyValuePair<String, String>> { };

            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(data))
            {
                result.Add(new KeyValuePair<string, string>(prop.Name, prop.GetValue(data).ToString()));
            }

            client.Content = new FormUrlEncodedContent(result);

            return client;

            #endregion
        }

        #endregion

        #region [ Respone ]

        /// <summary>
        /// Tenta deseralizar a resposta em caso de sucesso, se erro sempre devolve uma estancia padrao do Objeto
        /// </summary>
        /// <typeparam name="TConcrete"></typeparam>
        /// <param name="client"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public static async Task<TConcrete> DeserializeJsonResponse<TConcrete>(this HttpResponseMessage response)
        {
            return await response.DeserializeJsonResponse<TConcrete>(null);
        }

        /// <summary>
        /// Tenta deseralizar a resposta em caso de sucesso, se erro sempre devolve uma estancia padrao do Objeto
        /// </summary>
        /// <typeparam name="TConcrete"></typeparam>
        /// <param name="client"></param>
        /// <param name="response"></param>
        /// <param name="jsonSettings"></param>
        /// <returns></returns>
        public static async Task<TConcrete> DeserializeJsonResponse<TConcrete>(this HttpResponseMessage response, JsonSerializerSettings jsonSettings)
        {
            return await Task.Run(() => {
                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.BadRequest)
                {
                    try
                    {
                        jsonSettings = jsonSettings == null ? new JsonSerializerSettings() : jsonSettings;
                        jsonSettings.NullValueHandling = NullValueHandling.Ignore;
                        jsonSettings.Converters.Add(new DecimalJsonConverter());
                        jsonSettings.Converters.Add(new IntegerJsonConverter());

                        return JsonConvert.DeserializeObject<TConcrete>(response.Content.ReadAsStringAsync().Result, jsonSettings);
                    }
                    catch (Exception ex)
                    {
                        return (TConcrete)Activator.CreateInstance(typeof(TConcrete));
                    }
                }

                return (TConcrete)Activator.CreateInstance(typeof(TConcrete));
            });
        }

        #endregion

    }
}
