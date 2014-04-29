﻿// expr.cs
//
// Copyright 2011 Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

namespace Microsoft.NodejsTools.Parsing
{
    public abstract class Expression : Node
    {
        protected Expression(TokenWithSpan context, JSParser parser)
            : base(context, parser)
        {
        }

        public virtual bool ReplaceChild(Expression oldNode, Expression newNode) {
            return false;
        }

    }
}
