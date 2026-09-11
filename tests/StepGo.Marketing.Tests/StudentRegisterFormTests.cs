using Bunit;
using StepGo.Marketing.Client.Components;
using Xunit;

namespace StepGo.Marketing.Tests;

public class StudentRegisterFormTests : TestContext
{
    // frontend-auth spec: "不需送出表單或等待後端回應" — StudentRegisterForm has no
    // HttpClient/IHttpClientFactory dependency at all (validation is DataAnnotations only,
    // and submission just flips a local `_submitted` flag), so it is structurally incapable
    // of making a network call; no server/mock is running for any test in this class.


    [Fact]
    public void Blank_Phone_Blocks_Submit()
    {
        var cut = RenderComponent<StudentRegisterForm>();

        cut.Find("input").Change("王小明"); // name
        // phone left blank
        cut.Find("form").Submit();

        Assert.Contains("手機號碼為必填", cut.Markup);
        Assert.DoesNotContain("註冊完成", cut.Markup);
    }

    [Fact]
    public void Blank_Email_Still_Allows_Submit()
    {
        var cut = RenderComponent<StudentRegisterForm>();

        cut.FindAll("input")[0].Change("王小明"); // name
        cut.FindAll("input")[1].Change("0912345678"); // phone
        // email left blank
        cut.Find("form").Submit();

        Assert.Contains("註冊完成", cut.Markup);
    }

    [Fact]
    public void Invalid_Email_Format_Shows_Error_Immediately_On_Blur_Without_Submitting()
    {
        var cut = RenderComponent<StudentRegisterForm>();

        var emailInput = cut.FindAll("input")[2];
        emailInput.Change("not-an-email"); // triggers EditContext.OnFieldChanged (blur), not a submit

        Assert.Contains("Email 格式不正確", cut.Markup);
        Assert.DoesNotContain("註冊完成", cut.Markup); // form was never submitted
    }
}
