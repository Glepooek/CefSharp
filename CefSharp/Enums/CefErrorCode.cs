// Copyright © 2013 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

namespace CefSharp
{
    /// <summary>
    /// This file contains the list of network errors.
    /// 
    /// For a complete up-to-date list, see the CEF source code
    /// (cef_errorcode_t in include/internal/cef_types.h)
    /// and the Chromium source code (net/base/net_error_list.h).
    /// </summary>
    public enum CefErrorCode
    {
        /// <summary>
        /// No error occurred.
        /// </summary>
        None = 0,

        //
        // Ranges:
        //     0- 99 System related errors
        //   100-199 Connection related errors
        //   200-299 Certificate errors
        //   300-399 HTTP errors
        //   400-499 Cache errors
        //   500-599 ?
        //   600-699 <Obsolete: FTP errors>
        //   700-799 Certificate manager errors
        //   800-899 DNS resolver errors

        /// <summary>
        /// An asynchronous IO operation is not yet complete.  This usually does not
        /// indicate a fatal error.  Typically this error will be generated as a
        /// notification to wait for some external notification that the IO operation
        /// finally completed.
        /// </summary>
        IoPending = -1,

        /// <summary>
        /// A generic failure occurred.
        /// </summary>
        Failed = -2,

        /// <summary>
        /// An operation was aborted (due to user action).
        /// </summary>
        Aborted = -3,

        /// <summary>
        /// An argument to the function is incorrect.
        /// </summary>
        InvalidArgument = -4,

        /// <summary>
        /// The handle or file descriptor is invalid.
        /// </summary>
        InvalidHandle = -5,

        /// <summary>
        /// The file or directory cannot be found.
        /// </summary>
        FileNotFound = -6,

        /// <summary>
        /// An operation timed out.
        /// </summary>
        TimedOut = -7,

        /// <summary>
        /// The file is too large.
        /// </summary>
        FileTooBig = -8,

        /// <summary>
        /// An unexpected error.  This may be caused by a programming mistake or an
        /// invalid assumption.
        /// </summary>
        Unexpected = -9,

        /// <summary>
        /// Permission to access a resource, other than the network, was denied.
        /// </summary>
        AccessDenied = -10,

        /// <summary>
        /// The operation failed because of unimplemented functionality.
        /// </summary>
        NotImplemented = -11,

        /// <summary>
        /// There were not enough resources to complete the operation.
        /// </summary>
        InsufficientResources = -12,

        /// <summary>
        /// Memory allocation failed.
        /// </summary>
        OutOfMemory = -13,

        /// <summary>
        /// The file upload failed because the file's modification time was different
        /// from the expectation.
        /// </summary>
        UploadFileChanged = -14,

        /// <summary>
        /// The socket is not connected.
        /// </summary>
        SocketNotConnected = -15,

        /// <summary>
        /// The file already exists.
        /// </summary>
        FileExists = -16,

        /// <summary>
        /// The path or file name is too long.
        /// </summary>
        FilePathTooLong = -17,

        /// <summary>
        /// Not enough room left on the disk.
        /// </summary>
        FileNoSpace = -18,

        /// <summary>
        /// The file has a virus.
        /// </summary>
        FileVirusInfected = -19,

        /// <summary>
        /// The client chose to block the request.
        /// </summary>
        BlockedByClient = -20,

        /// <summary>
        /// The network changed.
        /// </summary>
        NetworkChanged = -21,

        /// <summary>
        /// The request was blocked by the URL block list configured by the domain
        /// administrator.
        /// </summary>
        BlockedByAdministrator = -22,

        /// <summary>
        /// The socket is already connected.
        /// </summary>
        SocketIsConnected = -23,

        // Error -24 was removed (BLOCKED_ENROLLMENT_CHECK_PENDING)

        /// <summary>
        /// The upload failed because the upload stream needed to be re-read, due to a
        /// retry or a redirect, but the upload stream doesn't support that operation.
        /// </summary>
        UploadStreamRewindNotSupported = -25,

        /// <summary>
        /// The request failed because the URLRequestContext is shutting down, or has
        /// been shut down.
        /// </summary>
        ContextShutDown = -26,

        /// <summary>
        /// The request failed because the response was delivered along with requirements
        /// which are not met ('X-Frame-Options' and 'Content-Security-Policy' ancestor
        /// checks and 'Cross-Origin-Resource-Policy' for instance).
        /// </summary>
        BlockedByResponse = -27,

        // Error -28 was removed (BLOCKED_BY_XSS_AUDITOR).

        /// <summary>
        /// The request was blocked by system policy disallowing some or all cleartext
        /// requests. Used for NetworkSecurityPolicy on Android.
        /// </summary>
        CleartextNotPermitted = -29,

        /// <summary>
        /// The request was blocked by a Content Security Policy
        /// </summary>
        BlockedByCsp = -30,

        /// <summary>
        /// The request was blocked because of no H/2 or QUIC session.
        /// </summary>
        H2OrQuicRequired = -31,

        /// <summary>
        /// The request was blocked by CORB or ORB.
        /// </summary>
        BlockedByOrb = -32,

        /// <summary>
        /// The request was blocked because it originated from a frame that has disabled
        /// network access.
        /// </summary>
        NetworkAccessRevoked = -33,

        /// <summary>
        /// The request was blocked by fingerprinting protections.
        /// </summary>
        BlockedByFingerprintingProtection = -34,

        /// <summary>
        /// A connection was closed (corresponding to a TCP FIN).
        /// </summary>
        ConnectionClosed = -100,

        /// <summary>
        /// A connection was reset (corresponding to a TCP RST).
        /// </summary>
        ConnectionReset = -101,

        /// <summary>
        /// A connection attempt was refused.
        /// </summary>
        ConnectionRefused = -102,

        /// <summary>
        /// A connection timed out as a result of not receiving an ACK for data sent.
        /// This can include a FIN packet that did not get ACK'd.
        /// </summary>
        ConnectionAborted = -103,

        /// <summary>
        /// A connection attempt failed.
        /// </summary>
        ConnectionFailed = -104,

        /// <summary>
        /// The host name could not be resolved.
        /// </summary>
        NameNotResolved = -105,

        /// <summary>
        /// The Internet connection has been lost.
        /// </summary>
        InternetDisconnected = -106,

        /// <summary>
        /// An SSL protocol error occurred.
        /// </summary>
        SslProtocolError = -107,

        /// <summary>
        /// The IP address or port number is invalid (e.g., cannot connect to the IP
        /// address 0 or the port 0).
        /// </summary>
        AddressInvalid = -108,

        /// <summary>
        /// The IP address is unreachable.  This usually means that there is no route to
        /// the specified host or network.
        /// </summary>
        AddressUnreachable = -109,

        /// <summary>
        /// The server requested a client certificate for SSL client authentication.
        /// </summary>
        SslClientAuthCertNeeded = -110,

        /// <summary>
        /// A tunnel connection through the proxy could not be established.
        /// </summary>
        TunnelConnectionFailed = -111,

        /// <summary>
        /// No SSL protocol versions are enabled.
        /// </summary>
        NoSslVersionsEnabled = -112,

        /// <summary>
        /// The client and server don't support a common SSL protocol version or
        /// cipher suite.
        /// </summary>
        SslVersionOrCipherMismatch = -113,

        /// <summary>
        /// The server requested a renegotiation (rehandshake).
        /// </summary>
        SslRenegotiationRequested = -114,

        /// <summary>
        /// The proxy requested authentication (for tunnel establishment) with an
        /// unsupported method.
        /// </summary>
        ProxyAuthUnsupported = -115,

        // Error -116 was removed (CERT_ERROR_IN_SSL_RENEGOTIATION)

        /// <summary>
        /// The SSL handshake failed because of a bad or missing client certificate.
        /// </summary>
        BadSslClientAuthCert = -117,

        /// <summary>
        /// A connection attempt timed out.
        /// </summary>
        ConnectionTimedOut = -118,

        /// <summary>
        /// There are too many pending DNS resolves, so a request in the queue was
        /// aborted.
        /// </summary>
        HostResolverQueueTooLarge = -119,

        /// <summary>
        /// Failed establishing a connection to the SOCKS proxy server for a target host.
        /// </summary>
        SocksConnectionFailed = -120,

        /// <summary>
        /// The SOCKS proxy server failed establishing connection to the target host
        /// because that host is unreachable.
        /// </summary>
        SocksConnectionHostUnreachable = -121,

        /// <summary>
        /// The request to negotiate an alternate protocol failed.
        /// </summary>
        AlpnNegotiationFailed = -122,

        /// <summary>
        /// The peer sent an SSL no_renegotiation alert message.
        /// </summary>
        SslNoRenegotiation = -123,

        /// <summary>
        /// Winsock sometimes reports more data written than passed.  This is probably
        /// due to a broken LSP.
        /// </summary>
        WinsockUnexpectedWrittenBytes = -124,

        /// <summary>
        /// An SSL peer sent us a fatal decompression_failure alert. This typically
        /// occurs when a peer selects DEFLATE compression in the mistaken belief that
        /// it supports it.
        /// </summary>
        SslDecompressionFailureAlert = -125,

        /// <summary>
        /// An SSL peer sent us a fatal bad_record_mac alert. This has been observed
        /// from servers with buggy DEFLATE support.
        /// </summary>
        SslBadRecordMacAlert = -126,

        /// <summary>
        /// The proxy requested authentication (for tunnel establishment).
        /// </summary>
        ProxyAuthRequested = -127,

        // Error -129 was removed (SSL_WEAK_SERVER_EPHEMERAL_DH_KEY).

        /// <summary>
        /// Could not create a connection to the proxy server. An error occurred
        /// either in resolving its name, or in connecting a socket to it.
        /// Note that this does NOT include failures during the actual "CONNECT" method
        /// of an HTTP proxy.
        /// </summary>
        ProxyConnectionFailed = -130,

        /// <summary>
        /// A mandatory proxy configuration could not be used. Currently this means
        /// that a mandatory PAC script could not be fetched, parsed or executed.
        /// </summary>
        MandatoryProxyConfigurationFailed = -131,

        // -132 was formerly ERR_ESET_ANTI_VIRUS_SSL_INTERCEPTION

        /// <summary>
        /// We've hit the max socket limit for the socket pool while preconnecting.  We
        /// don't bother trying to preconnect more sockets.
        /// </summary>
        PreconnectMaxSocketLimit = -133,

        /// <summary>
        /// The permission to use the SSL client certificate's private key was denied.
        /// </summary>
        SslClientAuthPrivateKeyAccessDenied = -134,

        /// <summary>
        /// The SSL client certificate has no private key.
        /// </summary>
        SslClientAuthCertNoPrivateKey = -135,

        /// <summary>
        /// The certificate presented by the HTTPS Proxy was invalid.
        /// </summary>
        ProxyCertificateInvalid = -136,

        /// <summary>
        /// An error occurred when trying to do a name resolution (DNS).
        /// </summary>
        NameResolutionFailed = -137,

        /// <summary>
        /// Permission to access the network was denied. This is used to distinguish
        /// errors that were most likely caused by a firewall from other access denied
        /// errors. See also ERR_ACCESS_DENIED.
        /// </summary>
        NetworkAccessDenied = -138,

        /// <summary>
        /// The request throttler module cancelled this request to avoid DDOS.
        /// </summary>
        TemporarilyThrottled = -139,

        /// <summary>
        /// A request to create an SSL tunnel connection through the HTTPS proxy
        /// received a 302 (temporary redirect) response.  The response body might
        /// include a description of why the request failed.
        ///
        /// TODO(crbug.com/40093955): This is deprecated and should not be used by
        /// new code.
        /// </summary>
        HttpsProxyTunnelResponseRedirect = -140,

        /// <summary>
        /// We were unable to sign the CertificateVerify data of an SSL client auth
        /// handshake with the client certificate's private key.
        ///
        /// Possible causes for this include the user implicitly or explicitly
        /// denying access to the private key, the private key may not be valid for
        /// signing, the key may be relying on a cached handle which is no longer
        /// valid, or the CSP won't allow arbitrary data to be signed.
        /// </summary>
        SslClientAuthSignatureFailed = -141,

        /// <summary>
        /// The message was too large for the transport.  (for example a UDP message
        /// which exceeds size threshold).
        /// </summary>
        MsgTooBig = -142,

        // Error -143 was removed (SPDY_SESSION_ALREADY_EXISTS)

        // Error -144 was removed (LIMIT_VIOLATION).

        /// <summary>
        /// Websocket protocol error. Indicates that we are terminating the connection
        /// due to a malformed frame or other protocol violation.
        /// </summary>
        WsProtocolError = -145,

        // Error -146 was removed (PROTOCOL_SWITCHED)

        /// <summary>
        /// Returned when attempting to bind an address that is already in use.
        /// </summary>
        AddressInUse = -147,

        /// <summary>
        /// An operation failed because the SSL handshake has not completed.
        /// </summary>
        SslHandshakeNotCompleted = -148,

        /// <summary>
        /// SSL peer's public key is invalid.
        /// </summary>
        SslBadPeerPublicKey = -149,

        /// <summary>
        /// The certificate didn't match the built-in public key pins for the host name.
        /// The pins are set in net/http/transport_security_state.cc and require that
        /// one of a set of public keys exist on the path from the leaf to the root.
        /// </summary>
        SslPinnedKeyNotInCertChain = -150,

        /// <summary>
        /// Server request for client certificate did not contain any types we support.
        /// </summary>
        ClientAuthCertTypeUnsupported = -151,

        // Error -152 was removed (ORIGIN_BOUND_CERT_GENERATION_TYPE_MISMATCH)

        /// <summary>
        /// An SSL peer sent us a fatal decrypt_error alert. This typically occurs when
        /// a peer could not correctly verify a signature (in CertificateVerify or
        /// ServerKeyExchange) or validate a Finished message.
        /// </summary>
        SslDecryptErrorAlert = -153,

        /// <summary>
        /// There are too many pending WebSocketJob instances, so the new job was not
        /// pushed to the queue.
        /// </summary>
        WsThrottleQueueTooLarge = -154,

        // Error -155 was removed (TOO_MANY_SOCKET_STREAMS)

        /// <summary>
        /// The SSL server certificate changed in a renegotiation.
        /// </summary>
        SslServerCertChanged = -156,

        // Error -157 was removed (SSL_INAPPROPRIATE_FALLBACK).

        // Error -158 was removed (CT_NO_SCTS_VERIFIED_OK).

        /// <summary>
        /// The SSL server sent us a fatal unrecognized_name alert.
        /// </summary>
        SslUnrecognizedNameAlert = -159,

        /// <summary>
        /// Failed to set the socket's receive buffer size as requested.
        /// </summary>
        SocketSetReceiveBufferSizeError = -160,

        /// <summary>
        /// Failed to set the socket's send buffer size as requested.
        /// </summary>
        SocketSetSendBufferSizeError = -161,

        /// <summary>
        /// Failed to set the socket's receive buffer size as requested, despite success
        /// return code from setsockopt.
        /// </summary>
        SocketReceiveBufferSizeUnchangeable = -162,

        /// <summary>
        /// Failed to set the socket's send buffer size as requested, despite success
        /// return code from setsockopt.
        /// </summary>
        SocketSendBufferSizeUnchangeable = -163,

        /// <summary>
        /// Failed to import a client certificate from the platform store into the SSL
        /// library.
        /// </summary>
        SslClientAuthCertBadFormat = -164,

        // Error -165 was removed (SSL_FALLBACK_BEYOND_MINIMUM_VERSION).

        /// <summary>
        /// Resolving a hostname to an IP address list included the IPv4 address
        /// "127.0.53.53". This is a special IP address which ICANN has recommended to
        /// indicate there was a name collision, and alert admins to a potential
        /// problem.
        /// </summary>
        ICANNNameCollision = -166,

        /// <summary>
        /// The SSL server presented a certificate which could not be decoded. This is
        /// not a certificate error code as no X509Certificate object is available. This
        /// error is fatal.
        /// </summary>
        SslServerCertBadFormat = -167,

        /// <summary>
        /// Certificate Transparency: Received a signed tree head that failed to parse.
        /// </summary>
        CtSthParsingFailed = -168,

        /// <summary>
        /// Certificate Transparency: Received a signed tree head whose JSON parsing was
        /// OK but was missing some of the fields.
        /// </summary>
        CtSthIncomplete = -169,

        /// <summary>
        /// The attempt to reuse a connection to send proxy auth credentials failed
        /// before the AuthController was used to generate credentials. The caller should
        /// reuse the controller with a new connection. This error is only used
        /// internally by the network stack.
        /// </summary>
        UnableToReuseConnectionForProxyAuth = -170,

        /// <summary>
        /// Certificate Transparency: Failed to parse the received consistency proof.
        /// </summary>
        CtConsistencyProofParsingFailed = -171,

        /// <summary>
        /// The SSL server required an unsupported cipher suite that has since been
        /// removed. This error will temporarily be signaled on a fallback for one or two
        /// releases immediately following a cipher suite's removal, after which the
        /// fallback will be removed.
        /// </summary>
        SslObsoleteCipher = -172,

        /// <summary>
        /// When a WebSocket handshake is done successfully and the connection has been
        /// upgraded, the URLRequest is cancelled with this error code.
        /// </summary>
        WsUpgrade = -173,

        /// <summary>
        /// Socket ReadIfReady support is not implemented. This error should not be user
        /// visible, because the normal Read() method is used as a fallback.
        /// </summary>
        ReadIfReadyNotImplemented = -174,

        // Error -175 was removed (SSL_VERSION_INTERFERENCE).

        /// <summary>
        /// No socket buffer space is available.
        /// </summary>
        NoBufferSpace = -176,

        /// <summary>
        /// There were no common signature algorithms between our client certificate
        /// private key and the server's preferences.
        /// </summary>
        SslClientAuthNoCommonAlgorithms = -177,

        /// <summary>
        /// TLS 1.3 early data was rejected by the server. This will be received before
        /// any data is returned from the socket. The request should be retried with
        /// early data disabled.
        /// </summary>
        EarlyDataRejected = -178,

        /// <summary>
        /// TLS 1.3 early data was offered, but the server responded with TLS 1.2 or
        /// earlier. This is an internal error code to account for a
        /// backwards-compatibility issue with early data and TLS 1.2. It will be
        /// received before any data is returned from the socket. The request should be
        /// retried with early data disabled.
        ///
        /// See https://tools.ietf.org/html/rfc8446#appendix-D.3 for details.
        /// </summary>
        WrongVersionOnEarlyData = -179,

        /// <summary>
        /// TLS 1.3 was enabled, but a lower version was negotiated and the server
        /// returned a value indicating it supported TLS 1.3. This is part of a security
        /// check in TLS 1.3, but it may also indicate the user is behind a buggy
        /// TLS-terminating proxy which implemented TLS 1.2 incorrectly. (See
        /// https://crbug.com/boringssl/226.)
        /// </summary>
        Tls13DowngradeDetected = -180,

        /// <summary>
        /// The server's certificate has a keyUsage extension incompatible with the
        /// negotiated TLS key exchange method.
        /// </summary>
        SslKeyUsageIncompatible = -181,

        /// <summary>
        /// The ECHConfigList fetched over DNS cannot be parsed.
        /// </summary>
        InvalidEchConfigList = -182,

        /// <summary>
        /// ECH was enabled, but the server was unable to decrypt the encrypted
        /// ClientHello.
        /// </summary>
        EchNotNegotiated = -183,

        /// <summary>
        /// ECH was enabled, the server was unable to decrypt the encrypted ClientHello,
        /// and additionally did not present a certificate valid for the public name.
        /// </summary>
        EchFallbackCertificateInvalid = -184,

        // Certificate error codes
        //
        // The values of certificate error codes must be consecutive.

        /// <summary>
        /// The server responded with a certificate whose common name did not match
        /// the host name.  This could mean:
        ///
        /// 1. An attacker has redirected our traffic to their server and is
        ///    presenting a certificate for which they know the private key.
        ///
        /// 2. The server is misconfigured and responding with the wrong cert.
        ///
        /// 3. The user is on a wireless network and is being redirected to the
        ///    network's login page.
        ///
        /// 4. The OS has used a DNS search suffix and the server doesn't have
        ///    a certificate for the abbreviated name in the address bar.
        ///
        /// </summary>
        CertCommonNameInvalid = -200,

        /// <summary>
        /// The server responded with a certificate that, by our clock, appears to
        /// either not yet be valid or to have expired.  This could mean:
        ///
        /// 1. An attacker is presenting an old certificate for which they have
        ///    managed to obtain the private key.
        ///
        /// 2. The server is misconfigured and is not presenting a valid cert.
        ///
        /// 3. Our clock is wrong.
        ///
        /// </summary>
        CertDateInvalid = -201,

        /// <summary>
        /// The server responded with a certificate that is signed by an authority
        /// we don't trust.  The could mean:
        ///
        /// 1. An attacker has substituted the real certificate for a cert that
        ///    contains their public key and is signed by their cousin.
        ///
        /// 2. The server operator has a legitimate certificate from a CA we don't
        ///    know about, but should trust.
        ///
        /// 3. The server is presenting a self-signed certificate, providing no
        ///    defense against active attackers (but foiling passive attackers).
        ///
        /// </summary>
        CertAuthorityInvalid = -202,

        /// <summary>
        /// The server responded with a certificate that contains errors.
        /// This error is not recoverable.
        ///
        /// MSDN describes this error as follows:
        ///   "The SSL certificate contains errors."
        /// NOTE: It's unclear how this differs from ERR_CERT_INVALID. For consistency,
        /// use that code instead of this one from now on.
        ///
        /// </summary>
        CertContainsErrors = -203,

        /// <summary>
        /// The certificate has no mechanism for determining if it is revoked.  In
        /// effect, this certificate cannot be revoked.
        /// </summary>
        CertNoRevocationMechanism = -204,

        /// <summary>
        /// Revocation information for the security certificate for this site is not
        /// available.  This could mean:
        ///
        /// 1. An attacker has compromised the private key in the certificate and is
        ///    blocking our attempt to find out that the cert was revoked.
        ///
        /// 2. The certificate is unrevoked, but the revocation server is busy or
        ///    unavailable.
        ///
        /// </summary>
        CertUnableToCheckRevocation = -205,

        /// <summary>
        /// The server responded with a certificate has been revoked.
        /// We have the capability to ignore this error, but it is probably not the
        /// thing to do.
        /// </summary>
        CertRevoked = -206,

        /// <summary>
        /// The server responded with a certificate that is invalid.
        /// This error is not recoverable.
        ///
        /// MSDN describes this error as follows:
        ///   "The SSL certificate is invalid."
        ///
        /// </summary>
        CertInvalid = -207,

        /// <summary>
        /// The server responded with a certificate that is signed using a weak
        /// signature algorithm.
        /// </summary>
        CertWeakSignatureAlgorithm = -208,

        // -209 is available: was CERT_NOT_IN_DNS.

        /// <summary>
        /// The host name specified in the certificate is not unique.
        /// </summary>
        CertNonUniqueName = -210,

        /// <summary>
        /// The server responded with a certificate that contains a weak key (e.g.
        /// a too-small RSA key).
        /// </summary>
        CertWeakKey = -211,

        /// <summary>
        /// The certificate claimed DNS names that are in violation of name constraints.
        /// </summary>
        CertNameConstraintViolation = -212,

        /// <summary>
        /// The certificate's validity period is too long.
        /// </summary>
        CertValidityTooLong = -213,

        /// <summary>
        /// Certificate Transparency was required for this connection, but the server
        /// did not provide CT information that complied with the policy.
        /// </summary>
        CertificateTransparencyRequired = -214,

        // Error -215 was removed (CERT_SYMANTEC_LEGACY)

        // -216 was QUIC_CERT_ROOT_NOT_KNOWN which has been renumbered to not be in the
        // certificate error range.

        /// <summary>
        /// The certificate is known to be used for interception by an entity other
        /// the device owner.
        /// </summary>
        CertKnownInterceptionBlocked = -217,

        // -218 was SSL_OBSOLETE_VERSION which is not longer used. TLS 1.0/1.1 instead
        // cause SSL_VERSION_OR_CIPHER_MISMATCH now.

        /// <summary>
        /// The certificate is self signed and it's being used for either an RFC1918 IP
        /// literal URL, or a url ending in .local.
        /// </summary>
        CertSelfSignedLocalNetwork = -219,

        // Add new certificate error codes here.
        //
        // Update the value of CERT_END whenever you add a new certificate error
        // code.

        /// <summary>
        /// The value immediately past the last certificate error code.
        /// </summary>
        CertEnd = -220,

        /// <summary>
        /// The URL is invalid.
        /// </summary>
        InvalidUrl = -300,

        /// <summary>
        /// The scheme of the URL is disallowed.
        /// </summary>
        DisallowedUrlScheme = -301,

        /// <summary>
        /// The scheme of the URL is unknown.
        /// </summary>
        UnknownUrlScheme = -302,

        /// <summary>
        /// Attempting to load an URL resulted in a redirect to an invalid URL.
        /// </summary>
        InvalidRedirect = -303,

        /// <summary>
        /// Attempting to load an URL resulted in too many redirects.
        /// </summary>
        TooManyRedirects = -310,

        /// <summary>
        /// Attempting to load an URL resulted in an unsafe redirect (e.g., a redirect
        /// to file:// is considered unsafe).
        /// </summary>
        UnsafeRedirect = -311,

        /// <summary>
        /// Attempting to load an URL with an unsafe port number.  These are port
        /// numbers that correspond to services, which are not robust to spurious input
        /// that may be constructed as a result of an allowed web construct (e.g., HTTP
        /// looks a lot like SMTP, so form submission to port 25 is denied).
        /// </summary>
        UnsafePort = -312,

        /// <summary>
        /// The server's response was invalid.
        /// </summary>
        InvalidResponse = -320,

        /// <summary>
        /// Error in chunked transfer encoding.
        /// </summary>
        InvalidChunkedEncoding = -321,

        /// <summary>
        /// The server did not support the request method.
        /// </summary>
        MethodNotSupported = -322,

        /// <summary>
        /// The response was 407 (Proxy Authentication Required), yet we did not send
        /// the request to a proxy.
        /// </summary>
        UnexpectedProxyAuth = -323,

        /// <summary>
        /// The server closed the connection without sending any data.
        /// </summary>
        EmptyResponse = -324,

        /// <summary>
        /// The headers section of the response is too large.
        /// </summary>
        ResponseHeadersTooBig = -325,

        // Error -326 was removed (PAC_STATUS_NOT_OK)

        /// <summary>
        /// The evaluation of the PAC script failed.
        /// </summary>
        PacScriptFailed = -327,

        /// <summary>
        /// The response was 416 (Requested range not satisfiable) and the server cannot
        /// satisfy the range requested.
        /// </summary>
        RequestRangeNotSatisfiable = -328,

        /// <summary>
        /// The identity used for authentication is invalid.
        /// </summary>
        MalformedIdentity = -329,

        /// <summary>
        /// Content decoding of the response body failed.
        /// </summary>
        ContentDecodingFailed = -330,

        /// <summary>
        /// An operation could not be completed because all network IO
        /// is suspended.
        /// </summary>
        NetworkIoSuspended = -331,

        /// <summary>
        /// FLIP data received without receiving a SYN_REPLY on the stream.
        /// </summary>
        SynReplyNotReceived = -332,

        /// <summary>
        /// Converting the response to target encoding failed.
        /// </summary>
        EncodingConversionFailed = -333,

        /// <summary>
        /// The server sent an FTP directory listing in a format we do not understand.
        /// </summary>
        UnrecognizedFtpDirectoryListingFormat = -334,

        // Obsolete.  Was only logged in NetLog when an HTTP/2 pushed stream expired.
        // NET_ERROR(INVALID_SPDY_STREAM, -335)

        /// <summary>
        /// There are no supported proxies in the provided list.
        /// </summary>
        NoSupportedProxies = -336,

        /// <summary>
        /// There is an HTTP/2 protocol error.
        /// </summary>
        Http2ProtocolError = -337,

        /// <summary>
        /// Credentials could not be established during HTTP Authentication.
        /// </summary>
        InvalidAuthCredentials = -338,

        /// <summary>
        /// An HTTP Authentication scheme was tried which is not supported on this
        /// machine.
        /// </summary>
        UnsupportedAuthScheme = -339,

        /// <summary>
        /// Detecting the encoding of the response failed.
        /// </summary>
        EncodingDetectionFailed = -340,

        /// <summary>
        /// (GSSAPI) No Kerberos credentials were available during HTTP Authentication.
        /// </summary>
        MissingAuthCredentials = -341,

        /// <summary>
        /// An unexpected, but documented, SSPI or GSSAPI status code was returned.
        /// </summary>
        UnexpectedSecurityLibraryStatus = -342,

        /// <summary>
        /// The environment was not set up correctly for authentication (for
        /// example, no KDC could be found or the principal is unknown.
        /// </summary>
        MisconfiguredAuthEnvironment = -343,

        /// <summary>
        /// An undocumented SSPI or GSSAPI status code was returned.
        /// </summary>
        UndocumentedSecurityLibraryStatus = -344,

        /// <summary>
        /// The HTTP response was too big to drain.
        /// </summary>
        ResponseBodyTooBigToDrain = -345,

        /// <summary>
        /// The HTTP response contained multiple distinct Content-Length headers.
        /// </summary>
        ResponseHeadersMultipleContentLength = -346,

        /// <summary>
        /// HTTP/2 headers have been received, but not all of them - status or version
        /// headers are missing, so we're expecting additional frames to complete them.
        /// </summary>
        IncompleteHttp2Headers = -347,

        /// <summary>
        /// No PAC URL configuration could be retrieved from DHCP. This can indicate
        /// either a failure to retrieve the DHCP configuration, or that there was no
        /// PAC URL configured in DHCP.
        /// </summary>
        PacNotInDhcp = -348,

        /// <summary>
        /// The HTTP response contained multiple Content-Disposition headers.
        /// </summary>
        ResponseHeadersMultipleContentDisposition = -349,

        /// <summary>
        /// The HTTP response contained multiple Location headers.
        /// </summary>
        ResponseHeadersMultipleLocation = -350,

        /// <summary>
        /// HTTP/2 server refused the request without processing, and sent either a
        /// GOAWAY frame with error code NO_ERROR and Last-Stream-ID lower than the
        /// stream id corresponding to the request indicating that this request has not
        /// been processed yet, or a RST_STREAM frame with error code REFUSED_STREAM.
        /// Client MAY retry (on a different connection).  See RFC7540 Section 8.1.4.
        /// </summary>
        Http2ServerRefusedStream = -351,

        /// <summary>
        /// HTTP/2 server didn't respond to the PING message.
        /// </summary>
        Http2PingFailed = -352,

        // Obsolete.  Kept here to avoid reuse, as the old error can still appear on
        // histograms.
        // NET_ERROR(PIPELINE_EVICTION, -353)

        /// <summary>
        /// The HTTP response body transferred fewer bytes than were advertised by the
        /// Content-Length header when the connection is closed.
        /// </summary>
        ContentLengthMismatch = -354,

        /// <summary>
        /// The HTTP response body is transferred with Chunked-Encoding, but the
        /// terminating zero-length chunk was never sent when the connection is closed.
        /// </summary>
        IncompleteChunkedEncoding = -355,

        /// <summary>
        /// There is a QUIC protocol error.
        /// </summary>
        QuicProtocolError = -356,

        /// <summary>
        /// The HTTP headers were truncated by an EOF.
        /// </summary>
        ResponseHeadersTruncated = -357,

        /// <summary>
        /// The QUIC crypto handshake failed.  This means that the server was unable
        /// to read any requests sent, so they may be resent.
        /// </summary>
        QuicHandshakeFailed = -358,

        // Obsolete.  Kept here to avoid reuse, as the old error can still appear on
        // histograms.
        // NET_ERROR(REQUEST_FOR_SECURE_RESOURCE_OVER_INSECURE_QUIC, -359)

        /// <summary>
        /// Transport security is inadequate for the HTTP/2 version.
        /// </summary>
        Http2InadequateTransportSecurity = -360,

        /// <summary>
        /// The peer violated HTTP/2 flow control.
        /// </summary>
        Http2FlowControlError = -361,

        /// <summary>
        /// The peer sent an improperly sized HTTP/2 frame.
        /// </summary>
        Http2FrameSizeError = -362,

        /// <summary>
        /// Decoding or encoding of compressed HTTP/2 headers failed.
        /// </summary>
        Http2CompressionError = -363,

        /// <summary>
        /// Proxy Auth Requested without a valid Client Socket Handle.
        /// </summary>
        ProxyAuthRequestedWithNoConnection = -364,

        /// <summary>
        /// HTTP_1_1_REQUIRED error code received on HTTP/2 session.
        /// </summary>
        Http11Required = -365,

        /// <summary>
        /// HTTP_1_1_REQUIRED error code received on HTTP/2 session to proxy.
        /// </summary>
        ProxyHttp11Required = -366,

        /// <summary>
        /// The PAC script terminated fatally and must be reloaded.
        /// </summary>
        PacScriptTerminated = -367,

        /// <summary>
        /// Signals that the request requires the IPP proxy.
        /// </summary>
        ProxyRequired = -368,

        // Obsolete. Kept here to avoid reuse.
        // Request is throttled because of a Backoff header.
        // See: crbug.com/486891.
        // NET_ERROR(TEMPORARY_BACKOFF, -369)

        /// <summary>
        /// The server was expected to return an HTTP/1.x response, but did not. Rather
        /// than treat it as HTTP/0.9, this error is returned.
        /// </summary>
        InvalidHttpResponse = -370,

        /// <summary>
        /// Initializing content decoding failed.
        /// </summary>
        ContentDecodingInitFailed = -371,

        /// <summary>
        /// Received HTTP/2 RST_STREAM frame with NO_ERROR error code.  This error should
        /// be handled internally by HTTP/2 code, and should not make it above the
        /// SpdyStream layer.
        /// </summary>
        Http2RstStreamNoErrorReceived = -372,

        // Obsolete. HTTP/2 push is removed.
        // NET_ERROR(HTTP2_PUSHED_STREAM_NOT_AVAILABLE, -373)

        // Obsolete. HTTP/2 push is removed.
        // NET_ERROR(HTTP2_CLAIMED_PUSHED_STREAM_RESET_BY_SERVER, -374)

        /// <summary>
        /// An HTTP transaction was retried too many times due for authentication or
        /// invalid certificates. This may be due to a bug in the net stack that would
        /// otherwise infinite loop, or if the server or proxy continually requests fresh
        /// credentials or presents a fresh invalid certificate.
        /// </summary>
        TooManyRetries = -375,

        /// <summary>
        /// Received an HTTP/2 frame on a closed stream.
        /// </summary>
        Http2StreamClosed = -376,

        // Obsolete. HTTP/2 push is removed.
        // NET_ERROR(HTTP2_CLIENT_REFUSED_STREAM, -377)

        // Obsolete. HTTP/2 push is removed.
        // NET_ERROR(HTTP2_PUSHED_RESPONSE_DOES_NOT_MATCH, -378)

        /// <summary>
        /// The server returned a non-2xx HTTP response code.
        ///
        /// Note that this error is only used by certain APIs that interpret the HTTP
        /// response itself. URLRequest for instance just passes most non-2xx
        /// response back as success.
        /// </summary>
        HttpResponseCodeFailure = -379,

        /// <summary>
        /// The certificate presented on a QUIC connection does not chain to a known root
        /// and the origin connected to is not on a list of domains where unknown roots
        /// are allowed.
        /// </summary>
        QuicCertRootNotKnown = -380,

        /// <summary>
        /// A GOAWAY frame has been received indicating that the request has not been
        /// processed and is therefore safe to retry on a different connection.
        /// </summary>
        QuicGoawayRequestCanBeRetried = -381,

        /// <summary>
        /// The ACCEPT_CH restart has been triggered too many times
        /// </summary>
        TooManyAcceptChRestarts = -382,

        /// <summary>
        /// The IP address space of the remote endpoint differed from the previous
        /// observed value during the same request. Any cache entry for the affected
        /// request should be invalidated.
        /// </summary>
        InconsistentIpAddressSpace = -383,

        /// <summary>
        /// The IP address space of the cached remote endpoint is blocked by private
        /// network access check.
        /// </summary>
        CachedIpAddressSpaceBlockedByPrivateNetworkAccessPolicy = -384,

        /// <summary>
        /// The connection is blocked by private network access checks.
        /// </summary>
        BlockedByPrivateNetworkAccessChecks = -385,

        /// <summary>
        /// Content decoding failed due to the zstd window size being too big (over 8MB).
        /// </summary>
        ZstdWindowSizeTooBig = -386,

        /// <summary>
        /// The compression dictionary cannot be loaded.
        /// </summary>
        DictionaryLoadFailed = -387,

        /// <summary>
        /// The header of dictionary compressed stream does not match the expected value.
        /// </summary>
        UnexpectedContentDictionaryHeader = -388,

        /// <summary>
        /// The cache does not have the requested entry.
        /// </summary>
        CacheMiss = -400,

        /// <summary>
        /// Unable to read from the disk cache.
        /// </summary>
        CacheReadFailure = -401,

        /// <summary>
        /// Unable to write to the disk cache.
        /// </summary>
        CacheWriteFailure = -402,

        /// <summary>
        /// The operation is not supported for this entry.
        /// </summary>
        CacheOperationNotSupported = -403,

        /// <summary>
        /// The disk cache is unable to open this entry.
        /// </summary>
        CacheOpenFailure = -404,

        /// <summary>
        /// The disk cache is unable to create this entry.
        /// </summary>
        CacheCreateFailure = -405,

        /// <summary>
        /// Multiple transactions are racing to create disk cache entries. This is an
        /// internal error returned from the HttpCache to the HttpCacheTransaction that
        /// tells the transaction to restart the entry-creation logic because the state
        /// of the cache has changed.
        /// </summary>
        CacheRace = -406,

        /// <summary>
        /// The cache was unable to read a checksum record on an entry. This can be
        /// returned from attempts to read from the cache. It is an internal error,
        /// returned by the SimpleCache backend, but not by any URLRequest methods
        /// or members.
        /// </summary>
        CacheChecksumReadFailure = -407,

        /// <summary>
        /// The cache found an entry with an invalid checksum. This can be returned from
        /// attempts to read from the cache. It is an internal error, returned by the
        /// SimpleCache backend, but not by any URLRequest methods or members.
        /// </summary>
        CacheChecksumMismatch = -408,

        /// <summary>
        /// Internal error code for the HTTP cache. The cache lock timeout has fired.
        /// </summary>
        CacheLockTimeout = -409,

        /// <summary>
        /// Received a challenge after the transaction has read some data, and the
        /// credentials aren't available.  There isn't a way to get them at that point.
        /// </summary>
        CacheAuthFailureAfterRead = -410,

        /// <summary>
        /// Internal not-quite error code for the HTTP cache. In-memory hints suggest
        /// that the cache entry would not have been usable with the transaction's
        /// current configuration (e.g. load flags, mode, etc.)
        /// </summary>
        CacheEntryNotSuitable = -411,

        /// <summary>
        /// The disk cache is unable to doom this entry.
        /// </summary>
        CacheDoomFailure = -412,

        /// <summary>
        /// The disk cache is unable to open or create this entry.
        /// </summary>
        CacheOpenOrCreateFailure = -413,

        /// <summary>
        /// The server's response was insecure (e.g. there was a cert error).
        /// </summary>
        InsecureResponse = -501,

        /// <summary>
        /// An attempt to import a client certificate failed, as the user's key
        /// database lacked a corresponding private key.
        /// </summary>
        NoPrivateKeyForCert = -502,

        /// <summary>
        /// An error adding a certificate to the OS certificate database.
        /// </summary>
        AddUserCertFailed = -503,

        /// <summary>
        /// An error occurred while handling a signed exchange.
        /// </summary>
        InvalidSignedExchange = -504,

        /// <summary>
        /// An error occurred while handling a Web Bundle source.
        /// </summary>
        InvalidWebBundle = -505,

        /// <summary>
        /// A Trust Tokens protocol operation-executing request failed for one of a
        /// number of reasons (precondition failure, internal error, bad response).
        /// </summary>
        TrustTokenOperationFailed = -506,

        /// <summary>
        /// When handling a Trust Tokens protocol operation-executing request, the system
        /// was able to execute the request's Trust Tokens operation without sending the
        /// request to its destination: for instance, the results could have been present
        /// in a local cache (for redemption) or the operation could have been diverted
        /// to a local provider (for "platform-provided" issuance).
        /// </summary>
        TrustTokenOperationSuccessWithoutSendingRequest = -507,

        // *** Code -600 is reserved (was FTP_PASV_COMMAND_FAILED). ***
        // *** Code -601 is reserved (was FTP_FAILED). ***
        // *** Code -602 is reserved (was FTP_SERVICE_UNAVAILABLE). ***
        // *** Code -603 is reserved (was FTP_TRANSFER_ABORTED). ***
        // *** Code -604 is reserved (was FTP_FILE_BUSY). ***
        // *** Code -605 is reserved (was FTP_SYNTAX_ERROR). ***
        // *** Code -606 is reserved (was FTP_COMMAND_NOT_SUPPORTED). ***
        // *** Code -607 is reserved (was FTP_BAD_COMMAND_SEQUENCE). ***

        /// <summary>
        /// PKCS #12 import failed due to incorrect password.
        /// </summary>
        Pkcs12ImportBadPassword = -701,

        /// <summary>
        /// PKCS #12 import failed due to other error.
        /// </summary>
        Pkcs12ImportFailed = -702,

        /// <summary>
        /// CA import failed - not a CA cert.
        /// </summary>
        ImportCaCertNotCa = -703,

        /// <summary>
        /// Import failed - certificate already exists in database.
        /// Note it's a little weird this is an error but reimporting a PKCS12 is ok
        /// (no-op).  That's how Mozilla does it, though.
        /// </summary>
        ImportCertAlreadyExists = -704,

        /// <summary>
        /// CA import failed due to some other error.
        /// </summary>
        ImportCaCertFailed = -705,

        /// <summary>
        /// Server certificate import failed due to some internal error.
        /// </summary>
        ImportServerCertFailed = -706,

        /// <summary>
        /// PKCS #12 import failed due to invalid MAC.
        /// </summary>
        Pkcs12ImportInvalidMac = -707,

        /// <summary>
        /// PKCS #12 import failed due to invalid/corrupt file.
        /// </summary>
        Pkcs12ImportInvalidFile = -708,

        /// <summary>
        /// PKCS #12 import failed due to unsupported features.
        /// </summary>
        Pkcs12ImportUnsupported = -709,

        /// <summary>
        /// Key generation failed.
        /// </summary>
        KeyGenerationFailed = -710,

        // Error -711 was removed (ORIGIN_BOUND_CERT_GENERATION_FAILED)

        /// <summary>
        /// Failure to export private key.
        /// </summary>
        PrivateKeyExportFailed = -712,

        /// <summary>
        /// Self-signed certificate generation failed.
        /// </summary>
        SelfSignedCertGenerationFailed = -713,

        /// <summary>
        /// The certificate database changed in some way.
        /// </summary>
        CertDatabaseChanged = -714,

        // Error -715 was removed (CHANNEL_ID_IMPORT_FAILED)

        /// <summary>
        /// The certificate verifier configuration changed in some way.
        /// </summary>
        CertVerifierChanged = -716,

        // DNS error codes.

        /// <summary>
        /// DNS resolver received a malformed response.
        /// </summary>
        DnsMalformedResponse = -800,

        /// <summary>
        /// DNS server requires TCP
        /// </summary>
        DnsServerRequiresTcp = -801,

        /// <summary>
        /// DNS server failed.  This error is returned for all of the following
        /// error conditions:
        /// 1 - Format error - The name server was unable to interpret the query.
        /// 2 - Server failure - The name server was unable to process this query
        ///     due to a problem with the name server.
        /// 4 - Not Implemented - The name server does not support the requested
        ///     kind of query.
        /// 5 - Refused - The name server refuses to perform the specified
        ///     operation for policy reasons.
        /// </summary>
        DnsServerFailed = -802,

        /// <summary>
        /// DNS transaction timed out.
        /// </summary>
        DnsTimedOut = -803,

        /// <summary>
        /// The entry was not found in cache or other local sources, for lookups where
        /// only local sources were queried.
        /// TODO(ericorth): Consider renaming to DNS_LOCAL_MISS or something like that as
        /// the cache is not necessarily queried either.
        /// </summary>
        DnsCacheMiss = -804,

        /// <summary>
        /// Suffix search list rules prevent resolution of the given host name.
        /// </summary>
        DnsSearchEmpty = -805,

        /// <summary>
        /// Failed to sort addresses according to RFC3484.
        /// </summary>
        DnsSortError = -806,

        // Error -807 was removed (DNS_HTTP_FAILED)

        /// <summary>
        /// Failed to resolve the hostname of a DNS-over-HTTPS server.
        /// </summary>
        DnsSecureResolverHostnameResolutionFailed = -808,

        /// <summary>
        /// DNS identified the request as disallowed for insecure connection (http/ws).
        /// Error should be handled as if an HTTP redirect was received to redirect to
        /// https or wss.
        /// </summary>
        DnsNameHttpsOnly = -809,

        /// <summary>
        /// All DNS requests associated with this job have been cancelled.
        /// </summary>
        DnsRequestCancelled = -810,

        /// <summary>
        /// The hostname resolution of HTTPS record was expected to be resolved with
        /// alpn values of supported protocols, but did not.
        /// </summary>
        DnsNoMatchingSupportedAlpn = -811,

        // Error -812 was removed
        // Error -813 was removed

        /// <summary>
        /// When checking whether secure DNS can be used, the response returned for the
        /// requested probe record either had no answer or was invalid.
        /// </summary>
        DnsSecureProbeRecordInvalid = -814,
    };
}
