using System.Diagnostics.CodeAnalysis;

namespace CloudEmail.SampleProject.API.Tests
{
    [ExcludeFromCodeCoverage]
    public static class TestConstants
    {
        /// <summary>
        /// Using the example public key documented:
        /// http://tlvconfluence01.nice.com:8090/pages/viewpage.action?pageId=192414359
        /// </summary>
        public static string FakeBase64EncodedX509PublicKeyString => "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAq21+RnULjpgGIHJBPHatbfIDtXP8GCD5nSw3vgvdv2LNDCeCFgrC7g4hQo1N4B3QvKldfm+6GyPhbl5YWVVvLwDP3QduN9nsXJJre1X7TDC3pUDDxws+VnD580EIirPbVwSFYxmaBXo4RUVB1n3yWZdiisgoVcYiK1dPMNfzHG5yWDXR9Gz/Nk8B3P9Ct7tCHOG0mVeRFL3peIxsLDbAvk4ApFf4xdZ2VEQ+mi4CtkNUR7PTp8jpD49QEBjNS2xiUWvPXs4KPMNd5L2FIgKZYlBPxvAhYgqYHk5cjIujrEMPblt5ndgPpIMkxk2K2pvCqszs7gFFZOoY9GQeF04GkwIDAQAB";

        public static string FakeCentralJwt => "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9.eyJpY0JVSWQiOjU2NTYsIm5hbWUiOiJtYXR0YXBpQGgxLmNvbSIsImlzcyI6Imh0dHBzOi8vYXBpLmluY29udGFjdC5jb20iLCJzdWIiOiJ1c2VyOjE0MjAiLCJhdWQiOiJpbnRlcm5hbEBpbkNvbnRhY3QgSW5jLiIsImV4cCI6MTUzMDI0NDAyMCwiaWF0IjoxNTMwMjQwNDIxLCJpY1Njb3BlIjoiNiIsImljQ2x1c3RlcklkIjoiSEMxIiwiaWNBZ2VudElkIjoxNDIwLCJpY1NQSWQiOjUsIm5iZiI6MTUzMDI0MDQyMH0.VONQFCFdh3Ec0QZVpx0vLsSfqZljRgcww8ZkgqFCELBE9V35BEAVzOu-yV7tQFeOQdWkhXcE6MWXcRdRN8OqUDHv2E4eswvlIAdZwvJvpbETuwrF06P0ijlxKUptLbZrdvbsuaZgWgC_G5wrMAFYUZgZ33u5xmKsb8Fbbd6XDrBDfumBHD5bQkusJCijIdATbkP0BELCNeSmBhYcdbMNzUD76DfZn98fvKFNzIeeG5b3Zwz-WxltxRjpRhXiaDd4x9xVxFsmgUkjodeXbiDgbRBkE00aZxLK6k4IHZMV3Ds24k9Zb1F3QjhZgTdAvlnR3NhlQMSw17ikqfuHKo136A";
        
    }
}
