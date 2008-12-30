﻿//-----------------------------------------------------------------------
// <copyright file="OpenIdMessageFactory.cs" company="Andrew Arnott">
//     Copyright (c) Andrew Arnott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DotNetOpenAuth.OpenId.ChannelElements {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using DotNetOpenAuth.Messaging;
	using DotNetOpenAuth.OpenId.Messages;
	using DotNetOpenAuth.OpenId.RelyingParty;

	/// <summary>
	/// Distinguishes the various OpenID message types for deserialization purposes.
	/// </summary>
	internal class OpenIdMessageFactory : IMessageFactory {
		#region IMessageFactory Members

		/// <summary>
		/// Analyzes an incoming request message payload to discover what kind of
		/// message is embedded in it and returns the type, or null if no match is found.
		/// </summary>
		/// <param name="recipient">The intended or actual recipient of the request message.</param>
		/// <param name="fields">The name/value pairs that make up the message payload.</param>
		/// <returns>
		/// A newly instantiated <see cref="IProtocolMessage"/>-derived object that this message can
		/// deserialize to.  Null if the request isn't recognized as a valid protocol message.
		/// </returns>
		public IDirectedProtocolMessage GetNewRequestMessage(MessageReceivingEndpoint recipient, IDictionary<string, string> fields) {
			ErrorUtilities.VerifyArgumentNotNull(recipient, "recipient");
			ErrorUtilities.VerifyArgumentNotNull(fields, "fields");

			RequestBase message = null;

			// Discern the OpenID version of the message.
			Protocol protocol = Protocol.V11;
			string ns;
			if (fields.TryGetValue(Protocol.V20.openid.ns, out ns)) {
				ErrorUtilities.VerifyProtocol(string.Equals(ns, Protocol.OpenId2Namespace, StringComparison.Ordinal), MessagingStrings.UnexpectedMessagePartValue, Protocol.V20.openid.ns, ns);
				protocol = Protocol.V20;
			}

			string mode;
			if (fields.TryGetValue(protocol.openid.mode, out mode)) {
				if (string.Equals(mode, protocol.Args.Mode.associate)) {
					if (fields.ContainsKey(protocol.openid.dh_consumer_public)) {
						message = new AssociateDiffieHellmanRequest(protocol.Version, recipient.Location);
					} else {
						message = new AssociateUnencryptedRequest(protocol.Version, recipient.Location);
					}
				} else if (string.Equals(mode, protocol.Args.Mode.checkid_setup) ||
					string.Equals(mode, protocol.Args.Mode.checkid_immediate)) {
					bool immediate = string.Equals(mode, protocol.Args.Mode.checkid_immediate);
					if (fields.ContainsKey(protocol.openid.identity)) {
						message = new CheckIdRequest(protocol.Version, recipient.Location, AuthenticationRequestMode.Immediate);
					} else {
						message = new SignedResponseRequest(protocol.Version, recipient.Location, AuthenticationRequestMode.Immediate);
					}
				} else if (string.Equals(mode, protocol.Args.Mode.cancel) ||
					(string.Equals(mode, protocol.Args.Mode.setup_needed) && (protocol.Version.Major >= 2 || fields.ContainsKey(protocol.openid.user_setup_url)))) {
					message = new NegativeAssertionResponse(protocol.Version, recipient.Location, mode);
				} else if (string.Equals(mode, protocol.Args.Mode.id_res)) {
					message = new PositiveAssertionResponse(protocol.Version, recipient.Location);
				} else if (string.Equals(mode, protocol.Args.Mode.check_authentication)) {
					message = new CheckAuthenticationRequest(protocol.Version, recipient.Location);
				} else {
					ErrorUtilities.ThrowProtocol(MessagingStrings.UnexpectedMessagePartValue, protocol.openid.mode, mode);
				}

				// TODO: handle more modes
			}

			if (message != null) {
				message.SetAsIncoming();
			}

			return message;
		}

		/// <summary>
		/// Analyzes an incoming request message payload to discover what kind of
		/// message is embedded in it and returns the type, or null if no match is found.
		/// </summary>
		/// <param name="request">The message that was sent as a request that resulted in the response.</param>
		/// <param name="fields">The name/value pairs that make up the message payload.</param>
		/// <returns>
		/// A newly instantiated <see cref="IProtocolMessage"/>-derived object that this message can
		/// deserialize to.  Null if the request isn't recognized as a valid protocol message.
		/// </returns>
		public IDirectResponseProtocolMessage GetNewResponseMessage(IDirectedProtocolMessage request, IDictionary<string, string> fields) {
			ErrorUtilities.VerifyArgumentNotNull(request, "request");
			ErrorUtilities.VerifyArgumentNotNull(fields, "fields");

			DirectResponseBase message = null;

			// Discern the OpenID version of the message.
			Protocol protocol = Protocol.V11;
			string ns;
			if (fields.TryGetValue(Protocol.V20.openidnp.ns, out ns)) {
				ErrorUtilities.VerifyProtocol(string.Equals(ns, Protocol.OpenId2Namespace, StringComparison.Ordinal), MessagingStrings.UnexpectedMessagePartValue, Protocol.V20.openidnp.ns, ns);
				protocol = Protocol.V20;
			}

			var associateDiffieHellmanRequest = request as AssociateDiffieHellmanRequest;
			var associateUnencryptedRequest = request as AssociateUnencryptedRequest;
			var checkAuthenticationRequest = request as CheckAuthenticationRequest;

			if (associateDiffieHellmanRequest != null) {
				message = new AssociateDiffieHellmanResponse(associateDiffieHellmanRequest);
			}

			if (associateUnencryptedRequest != null) {
				message = new AssociateUnencryptedResponse(associateUnencryptedRequest);
			}

			if (checkAuthenticationRequest != null) {
				message = new CheckAuthenticationResponse(checkAuthenticationRequest);
			}

			if (message != null) {
				message.SetAsIncoming();
			}

			return message;
		}

		#endregion
	}
}
