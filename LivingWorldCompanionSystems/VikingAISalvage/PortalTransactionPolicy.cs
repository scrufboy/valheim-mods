using System;
using System.Collections.Generic;

namespace LivingWorld.Companions.VikingAISalvage
{
    public enum PortalTransferState
    {
        Requested,
        DestinationValidated,
        DestinationReady,
        Committed,
        Completed,
        Rejected,
        RolledBack
    }

    public sealed class PortalTransferRequest
    {
        public string Identity;
        public string SourcePortal;
        public string DestinationPortal;
        public string RequestToken;
    }

    public sealed class PortalTransfer
    {
        public PortalTransferRequest Request { get; internal set; }
        public PortalTransferState State { get; internal set; }
        public string Failure { get; internal set; }
    }

    public sealed class PortalTransactionGate
    {
        private readonly HashSet<string> _seenTokens = new HashSet<string>(StringComparer.Ordinal);

        public PortalTransfer Begin(PortalTransferRequest request, bool legallyAllowed)
        {
            if (request == null) throw new ArgumentNullException("request");
            PortalTransfer transfer = new PortalTransfer { Request = request, State = PortalTransferState.Requested };
            if (string.IsNullOrEmpty(request.Identity) || string.IsNullOrEmpty(request.SourcePortal) ||
                string.IsNullOrEmpty(request.DestinationPortal) || string.IsNullOrEmpty(request.RequestToken))
                return Reject(transfer, "incomplete-request");
            if (!legallyAllowed) return Reject(transfer, "legal-restriction");
            if (!_seenTokens.Add(request.RequestToken)) return Reject(transfer, "duplicate-request");
            transfer.State = PortalTransferState.DestinationValidated;
            return transfer;
        }

        public bool MarkDestinationReady(PortalTransfer transfer, bool identityReserved, bool instanceReady)
        {
            if (!At(transfer, PortalTransferState.DestinationValidated)) return false;
            if (!identityReserved || !instanceReady)
            {
                Reject(transfer, identityReserved ? "destination-not-ready" : "identity-not-reserved");
                return false;
            }
            transfer.State = PortalTransferState.DestinationReady;
            return true;
        }

        public bool Commit(PortalTransfer transfer)
        {
            if (!At(transfer, PortalTransferState.DestinationReady)) return false;
            transfer.State = PortalTransferState.Committed;
            return true;
        }

        public bool Complete(PortalTransfer transfer)
        {
            if (!At(transfer, PortalTransferState.Committed)) return false;
            transfer.State = PortalTransferState.Completed;
            return true;
        }

        public bool RollBack(PortalTransfer transfer)
        {
            if (transfer == null || transfer.State == PortalTransferState.Completed ||
                transfer.State == PortalTransferState.Rejected || transfer.State == PortalTransferState.RolledBack)
                return false;
            transfer.State = PortalTransferState.RolledBack;
            return true;
        }

        private static bool At(PortalTransfer transfer, PortalTransferState expected)
        {
            return transfer != null && transfer.State == expected;
        }

        private static PortalTransfer Reject(PortalTransfer transfer, string reason)
        {
            transfer.State = PortalTransferState.Rejected;
            transfer.Failure = reason;
            return transfer;
        }
    }
}
