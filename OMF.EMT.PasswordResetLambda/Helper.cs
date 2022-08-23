using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Amazon.Lambda.Core;
using OMF.Amazon.Lambda.LexV2Events;
using OMF.Amazon.Lambda.LexV2FlowControl;

using GliaApiHelper;

namespace OMF.EMT.PasswordResetLambda
{
    public static class Helper
    {
        public static bool IsWorkingHours()
        {
            var now = DateTime.UtcNow;
            var tz = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
            now = TimeZoneInfo.ConvertTimeFromUtc(now, tz);

            if (Environment.GetEnvironmentVariable("Debug_OverrideTime") != null)
            {
                LambdaLogger.Log("OMF.EMT.PasswordResetLambda.Helper.IsWorkingHours - Overriding the time from environment Debug_OverrideTime");

                var dateString = Environment.GetEnvironmentVariable("Debug_OverrideTime");

                if (!DateTime.TryParse(dateString, out _))
                {
                    LambdaLogger.Log($"OMF.EMT.PasswordResetLambda.Helper.IsWorkingHours - Could not parse \"{dateString}\" into a date, override ignored.");
                }
                else
                {
                    now = DateTime.Parse(dateString);
                    LambdaLogger.Log($"OMF.EMT.PasswordResetLambda.Helper.IsWorkingHours - Date set to {now}");
                }
            }

            if ((now.DayOfWeek == DayOfWeek.Saturday) || (now.DayOfWeek == DayOfWeek.Sunday))
            {
                return false;
            }
            else if ((now.Hour <= 8) || (now.Hour >= 16))
            {
                return false;
            }
            else
                return true;
        }

        public static bool IsAgentAvailable()
        {
            var agentQueueId = Environment.GetEnvironmentVariable("AgentQueueId");
            var siteId = Environment.GetEnvironmentVariable("SiteId");
            var teamId = Environment.GetEnvironmentVariable("TeamId");

            LambdaLogger.Log($"IsAgentAvailable - agentQueueId {agentQueueId}, siteId {siteId}, teamId {teamId}");

            var glia = new GliaApiHelperSvc();

            (var queueStatus, var statusError) = glia.fetchQueueStatus(agentQueueId);

            LambdaLogger.Log($"fetchQueueStatus: queueStatus {queueStatus} : statusError {statusError}");

            if (!queueStatus.Equals("opened") || !string.IsNullOrEmpty(statusError))
                return false;

            (var isAgentAvailable, var operatorError) = glia.GetUnengagedOperators(siteId, teamId);

            LambdaLogger.Log($"GetUnengagedOperators: isAgentAvailable {isAgentAvailable} : operatorError {operatorError}");

            return (isAgentAvailable && string.IsNullOrEmpty(operatorError));
        }

        public static void TransferToAgent(LexV2Response response)
        {
            response.SessionState.DialogAction.Type = DialogActionType.Close;

            if (IsWorkingHours())
            {
                if (IsAgentAvailable())
                {
                    response.AddMessage("A Team Member is available and ready to help you. The next message will be from them.");
                    response.AddMessage(GliaAwsHelper.TextPromptAgentTransfer(GliaAwsHelper.TransferMediaType.Text, Environment.GetEnvironmentVariable("AgentQueueId")), ContentType.CustomPayload);
                }
                else
                {
                    response.AddMessage("We'd be happy to help, but all of our Team Members are busy with other customers. Please call us at 800-290-7002 M-F 8am-8pm EST or chat us again MF 8am-4pm EST. Thank you.");
                }
            }
            else
            {
                response.AddMessage("We'd be happy to help you during normal business hours M-F 8am-4pm EST. Thank you.");
                response.AddMessage(GliaAwsHelper.TextPromptEndEngagement(), ContentType.CustomPayload);
            }
        }
    }
}
