﻿using System;

using Websms;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create the client.
            SmsClient client = new SmsClient("username", "password", "https://api.websms.com/json");

            // Create new message with one recipient.
            TextMessage textMessage = new TextMessage(4367612345678, "Hello World!");

            try
            {
                // If test = true, it's just a test. No real sms will be sent.
                bool test = false;

                // Send the message.
                MessageResponse response = client.Send(textMessage, 1, test);
                // Print the response.
                Console.WriteLine("Status message: " + response.statusMessage);
                Console.WriteLine("Status code: " + response.statusCode);
            }
            catch (Exception ex)
            {
                // Handle exceptions.
                Console.WriteLine(ex.Message);
            }

            Console.ReadKey();

            Console.WriteLine("Hello World!");
        }
    }
}
