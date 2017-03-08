﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using MailValidation.Verify.Exceptions;
using MailValidation.Verify.SMTP;

namespace MailValidation.Verify
{
  public  class MailVerify
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        /// <exception cref="MailValidationException"></exception>
        public ValidationStatus Validate(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return ValidationStatus.AddressIsEmpty;
            }

            email = email.Trim();

            MailAddress mailAddress = null;

            try
            {
                mailAddress = new MailAddress(email);
            }
            catch (ArgumentNullException e)
            {
                return  ValidationStatus.AddressIsEmpty;
            }
            catch (ArgumentException e)
            {
                return ValidationStatus.AddressIsEmpty;
 }
            catch (FormatException e)
            {
                return ValidationStatus.InvalidFormat;
            }

            if (mailAddress.Address != email)
            {
                return ValidationStatus.InvalidFormat;
            }

            //////////////////

            DomainName domainName = DomainName.Parse(mailAddress.Host);
            DnsMessage dnsResponse = DnsClient.Default.Resolve(domainName, RecordType.Mx);

            IList<MxRecord> mxRecords = dnsResponse.AnswerRecords.OfType<MxRecord>().ToList();

            if (mxRecords.Count == 0)
            {
                return ValidationStatus.NoMailForDomain;

            }

            foreach (MxRecord mxRecord in mxRecords)
            {
                try
                {
                    SmtpService smtpService = new SmtpService(mxRecord.ExchangeDomainName.ToString());
                    SmtpStatusCode resultCode;

                    if (smtpService.CheckMailboxExists(email, out resultCode))
                    {
                        switch (resultCode)
                        {
                            case SmtpStatusCode.Ok:
                                result = ValidationStatus.OK;
                                return true;

                            case SmtpStatusCode.ExceededStorageAllocation:
                                result = ValidationStatus.MailboxStorageExceeded;
                                return true;

                            case SmtpStatusCode.MailboxUnavailable:
                                result = ValidationStatus.MailboxUnavailable;
                                return true;
                        }
                    }
                }
                catch (SmtpClientException)
                {
                }
                catch (ArgumentNullException)
                {
                }
            }

            if (mxRecords.Count > 0)
            {
                result = ValidationStatus.MailServerUnavailable;
                return false;
            }

            result = ValidationStatus.Undefined;
            return false;
        }

    }
}
