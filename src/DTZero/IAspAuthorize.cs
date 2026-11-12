
namespace DTZero;
public interface IAspAuthorize
{
	Task<string?> Authorize(IEnumerable<AspAuthorizeData> authorizeData, bool forbid = false);
}