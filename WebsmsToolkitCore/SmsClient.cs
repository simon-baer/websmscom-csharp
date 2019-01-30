/**
 * Copyright (C) 2012, sms.at mobile internet services gmbh
 * 
 * @author Markus Opitz
 */

using System;
using System.IO;
using System.Net;
using System.Text;

using Websms.Exceptions;
using Websms.Interfaces;
using Websms.Utils;

namespace Websms
{
    /**
     * @class SmsClient
     * @brief Containts methods for sending SMS messages.
     */
    public class SmsClient
    {
        /**
        * library version
        */
        private static string VERSION = "2.1.0";

        private readonly string username;
        private readonly string password;
        private readonly string url;
        private readonly IWebProxy proxy;

        /**
         * Constructor
         * @param[in] username User name
         * @param[in] password Password
         * @param[in] url URL
    	 * @param[in] proxy optionally an IWebProxy
         */
        public SmsClient(string username, string password, string url, IWebProxy proxy = null)
        {
            this.username = username;
            this.password = password;
            this.url = url.EndsWith("/") ? url : url + "/";
            this.proxy = proxy;
        }

        /**
         * Sends text message.
         * @param[in] message Text message
         * @param[in] maxSmsPerMessage Max sms per message
         * @param[in] test Test flag
         * @return Response
         */
        public MessageResponse Send(TextMessage message, uint maxSmsPerMessage, bool test)
        {
            ValidateCredentials();
            ValidateMessage(message);

            TextMessage tmp = Clone(message);
            tmp.maxSmsPerMessage = maxSmsPerMessage;
            tmp.test = test;

            string response = Post(tmp);
            return ValidateMessageResponse(JsonHelper.Deserialize<MessageResponse>(response));
        }

        /**
         * Sends binary message.
         * @param[in] message Binary message
         * @param[in] test Test flag
         * @return Response
         */
        public MessageResponse Send(BinaryMessage message, bool test)
        {
            ValidateCredentials();
            ValidateMessage(message);

            BinaryMessage tmp = Clone(message);
            tmp.test = test;

            string response = Post(tmp);
            return ValidateMessageResponse(JsonHelper.Deserialize<MessageResponse>(response));
        }

        /**
         * Validate credentials.
         * @throws Websms.Exceptions.AutorizationFailedException
         */
        private void ValidateCredentials()
        {
            if (username == null || username.Length == 0
                || password == null || password.Length == 0)
            {
                throw new AuthorizationFailedException("Missing username and/or password.");
            }
        }

        /**
         * Validate message.
         * @throws Websms.Exceptions.ParameterValidationException
         */
        private void ValidateMessage(TextMessage message) 
        {
            if (message.messageContent == null || message.messageContent.Length == 0)
            {
                throw new ParameterValidationException("No message content.");
            }
            else if (message.recipientAddressList == null || message.recipientAddressList.Length == 0)
            {
                throw new ParameterValidationException("No recipients");
            }
        }

        /**
         * Validate message.
         * @throws Websms.Exceptions.ParameterValidationException
         */
        private void ValidateMessage(BinaryMessage message)
        {
            if (message.messageContent == null || message.messageContent.Length == 0)
            {
                throw new ParameterValidationException("No message content.");
            }
            else if (message.recipientAddressList == null || message.recipientAddressList.Length == 0)
            {
                throw new ParameterValidationException("No recipients");
            }
        }

        /**
         * Validate message response.
         * @throws Websms.Exceptions.ApiException
         */
        private MessageResponse ValidateMessageResponse(MessageResponse response)
        {
            switch (response.statusCode)
            {
                case StatusCode.OK:
                case StatusCode.OK_QUEUED:
                case StatusCode.OK_TEST:
                {
                    break;
                }
                default:
                {
                    throw new ApiException(response.statusMessage, response.statusCode);
                }
            }

            return response;
        }

        /**
         * Clones object.
         * @param[in] data Source object
         * @return Object clone
         */
        private T Clone<T>(T data)
        {
            return JsonHelper.Deserialize<T>(JsonHelper.Serialize<T>(data));
        }

        /**
         * Post request.
         * @param[in] data Request
         * @return Response string
         */
        private string Post(IRequest data)
        {
            string rtn = null;
            byte[] bytes = Encoding.UTF8.GetBytes(JsonHelper.Serialize(data));
            string url = this.url + data.GetTargetUrl();

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json";
                if (this.proxy != null)
                {
                    request.Proxy = this.proxy;
                }

                string auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password));
                request.Headers["Authorization"] = "Basic " + auth;
                request.UserAgent = "dotnet core SDK Client (v"+ VERSION +")";

                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Close();
                }

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                using (Stream stream = response.GetResponseStream())
                {
                    if (stream != null)
                    {
                        StreamReader reader = new StreamReader(stream);
                        rtn = reader.ReadToEnd();
                        reader.Close();
                        stream.Close();
                    }
                }
                response.Close();
            }
            catch (WebException ex)
            {
                HttpStatusCode statusCode = ((HttpWebResponse)ex.Response).StatusCode;
                if (statusCode == HttpStatusCode.Unauthorized)
                {
                    throw new AuthorizationFailedException("Authorization failed.");
                }
                else
                {
                    throw new HttpConnectionException("HTTP request failed.", (int)statusCode);
                }
            }
            catch (Exception ex)
            {
                throw new HttpConnectionException(ex.Message, 0);
            }
               
            return rtn;
        }
    }
}
