// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudioTools.MockVsTests
{
    internal class MockOleComponentManager : IOleComponentManager
    {
        private readonly Dictionary<uint, IOleComponent> _idleComponents = new Dictionary<uint, IOleComponent>();
        private uint _idleCount;

        public int FContinueIdle()
        {
            throw new NotImplementedException();
        }

        public int FCreateSubComponentManager(object piunkOuter, object piunkServProv, ref Guid riid, out IntPtr ppvObj)
        {
            throw new NotImplementedException();
        }

        public int FGetActiveComponent(uint dwgac, out IOleComponent ppic, OLECRINFO[] pcrinfo, uint dwReserved)
        {
            throw new NotImplementedException();
        }

        public int FGetParentComponentManager(out IOleComponentManager ppicm)
        {
            throw new NotImplementedException();
        }

        public int FInState(uint uStateID, IntPtr pvoid)
        {
            throw new NotImplementedException();
        }

        public int FOnComponentActivate(uint dwComponentID)
        {
            throw new NotImplementedException();
        }

        public int FOnComponentExitState(uint dwComponentID, uint uStateID, uint uContext, uint cpicmExclude, IOleComponentManager[] rgpicmExclude)
        {
            throw new NotImplementedException();
        }

        public int FPushMessageLoop(uint dwComponentID, uint uReason, IntPtr pvLoopData)
        {
            throw new NotImplementedException();
        }

        public int FRegisterComponent(IOleComponent piComponent, OLECRINFO[] pcrinfo, out uint pdwComponentID)
        {
            var flags = (_OLECRF)pcrinfo[0].grfcrf;
            if (flags.HasFlag(_OLECRF.olecrfNeedIdleTime))
            {
                _idleComponents[++_idleCount] = piComponent;
                pdwComponentID = _idleCount;
            }
            else
            {
                throw new NotImplementedException();
            }
            return VSConstants.S_OK;
        }

        public int FReserved1(uint dwReserved, uint message, IntPtr wParam, IntPtr lParam)
        {
            throw new NotImplementedException();
        }

        public int FRevokeComponent(uint dwComponentID)
        {
            _idleComponents.Remove(dwComponentID);
            return VSConstants.S_OK;
        }

        public int FSetTrackingComponent(uint dwComponentID, int fTrack)
        {
            throw new NotImplementedException();
        }

        public int FUpdateComponentRegistration(uint dwComponentID, OLECRINFO[] pcrinfo)
        {
            throw new NotImplementedException();
        }

        public void OnComponentEnterState(uint dwComponentID, uint uStateID, uint uContext, uint cpicmExclude, IOleComponentManager[] rgpicmExclude, uint dwReserved)
        {
            throw new NotImplementedException();
        }

        public void QueryService(ref Guid guidService, ref Guid iid, out IntPtr ppvObj)
        {
            throw new NotImplementedException();
        }
    }
}

