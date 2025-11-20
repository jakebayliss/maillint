using Microsoft.AspNetCore.Mvc;
using API.Services;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RewriteController : ControllerBase
{
	private readonly LLMClient _llm;

	public RewriteController(LLMClient llm)
	{
		_llm = llm;
	}

	[HttpPost]
	public async Task<IActionResult> Rewrite([FromBody] RewriteRequest req, CancellationToken ct)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(req?.Text))
			{
				return BadRequest(new { error = "Text is required." });
			}

			var prompt = $$"""
			Rewrite the following email message. Tip: Keep everything concise and clear.
			Follow the below rules which apply to the given email:
			- If there are tasks, make sure they are numbered and clear
			- If the email is talking about a previous conversation, start the email with 'As per our previous conversation, ...'
			- If the email has been checked by another person, add a (Checked by <name>) at the top of the email
			- If the email is suggesting some kind of change, make sure to format in 'Change from <old> to <new>' format
			- If there are tasks for multiple people, make sure each person is addressed separately, almost like 2 emails in 1
			- When replying to a certain question or task, make sure to indent and prefix what you are replying to with a '>'. e.g. > 1. Please open the door
			- When replying to a task, prefix with the response. e.g. ✅ Done - <reasoning, if any>, ❌ Not Done - <reasoning, any>, etc
			- I may give you all history so you can see what we are replying to, do not update history, only update my message.
		
			Here are some examples of a an emails following the SSW rules (a real one wont have all):
			
			<email - Simple example - Request>
			Hey Jake,

				1. Could you please turn the office alarm on when you leave?
				
			Thanks,
			Rob
			</email>

			<email - Simple example - Response>
			Hey Rob,
			
				> 1. Could you please turn the office alarm on when you leave?
			✅ Done - Alarm is now off

			Thanks
			</email>

			<email - Complex example - Request>
			(Checked by Jake)
		
			Hi Calum,
		
			As per our conversation, you have agreed to the following changes of the signage:
			Change from:
				SSW
			Change to:
				SSW - NSW
		
			1. Please make this change as soon as possible.
		
			Thanks
			</email>
		
			<email - Complex example - Response>
			Hi Jake,
		
				> 1. Please make this change as soon as possible.
			✅ Done - Please see <link>
		
			Thanks
			</email>

			Email:
			{{req.Text}}
			""";

			var content = await _llm.CompleteAsync(prompt, ct);

			if (string.IsNullOrWhiteSpace(content))
			{
				return Problem("No content returned from the model.");
			}

			return Ok(new { text = content });
		}
		catch (Exception e)
		{
			return Problem(e.Message);
		}
	}
}

public record RewriteRequest(string Text);

