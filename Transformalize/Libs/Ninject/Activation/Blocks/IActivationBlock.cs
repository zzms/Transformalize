#region License
// 
// Author: Nate Kohari <nate@enkari.com>
// Copyright (c) 2007-2010, Enkari, Ltd.
// 
// Dual-licensed under the Apache License, Version 2.0, and the Microsoft Public License (Ms-PL).
// See the file LICENSE.txt for details.
// 
#endregion
#region Using Directives

using Transformalize.Libs.Ninject.Infrastructure.Disposal;
using Transformalize.Libs.Ninject.Syntax;

#endregion

namespace Transformalize.Libs.Ninject.Activation.Blocks
{
    /// <summary>
    /// A block used for deterministic disposal of activated instances. When the block is
    /// disposed, all instances activated via it will be deactivated.
    /// </summary>
    public interface IActivationBlock : IResolutionRoot, INotifyWhenDisposed { }
}