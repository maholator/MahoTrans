using MahoTrans.Native;

namespace javax.microedition.io;

[JavaInterface]
public interface HttpConnection : ContentConnection
{
    public const String GET = "GET";
    public const String HEAD = "HEAD";
    public const int HTTP_ACCEPTED = 202;
    public const int HTTP_BAD_GATEWAY = 502;
    public const int HTTP_BAD_METHOD = 405;
    public const int HTTP_BAD_REQUEST = 400;
    public const int HTTP_CLIENT_TIMEOUT = 408;
    public const int HTTP_CONFLICT = 409;
    public const int HTTP_CREATED = 201;
    public const int HTTP_ENTITY_TOO_LARGE = 413;
    public const int HTTP_EXPECT_FAILED = 417;
    public const int HTTP_FORBIDDEN = 403;
    public const int HTTP_GATEWAY_TIMEOUT = 504;
    public const int HTTP_GONE = 410;
    public const int HTTP_INTERNAL_ERROR = 500;
    public const int HTTP_LENGTH_REQUIRED = 411;
    public const int HTTP_MOVED_PERM = 301;
    public const int HTTP_MOVED_TEMP = 302;
    public const int HTTP_MULT_CHOICE = 300;
    public const int HTTP_NO_CONTENT = 204;
    public const int HTTP_NOT_ACCEPTABLE = 406;
    public const int HTTP_NOT_AUTHORITATIVE = 203;
    public const int HTTP_NOT_FOUND = 404;
    public const int HTTP_NOT_IMPLEMENTED = 501;
    public const int HTTP_NOT_MODIFIED = 304;
    public const int HTTP_OK = 200;
    public const int HTTP_PARTIAL = 206;
    public const int HTTP_PAYMENT_REQUIRED = 402;
    public const int HTTP_PRECON_FAILED = 412;
    public const int HTTP_PROXY_AUTH = 407;
    public const int HTTP_REQ_TOO_LONG = 414;
    public const int HTTP_RESET = 205;
    public const int HTTP_SEE_OTHER = 303;
    public const int HTTP_TEMP_REDIRECT = 307;
    public const int HTTP_UNAUTHORIZED = 401;
    public const int HTTP_UNAVAILABLE = 503;
    public const int HTTP_UNSUPPORTED_RANGE = 416;
    public const int HTTP_UNSUPPORTED_TYPE = 415;
    public const int HTTP_USE_PROXY = 305;
    public const int HTTP_VERSION = 505;
    public const String POST = "POST";
}
