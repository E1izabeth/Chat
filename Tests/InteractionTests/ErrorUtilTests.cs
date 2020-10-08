using Chat.Common;
using Chat.Interaction;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Tests.InteractionTests
{
    [TestClass]
    public class ErrorUtilTests
    {

        private void MakeException2(int depth = 20)
        {
            if (depth <= 0)
                throw new ApplicationException("11111");

            this.MakeException2(depth - 1);
        }

        private void MakeException(int depth = 20)
        {
            if (depth <= 0)
            {
                try
                {
                    this.MakeException2();
                }
                catch (Exception ex)
                {
                    throw new ApplicationException("22222", ex);
                }
            }

            this.MakeException(depth - 1);
        }

        [TestMethod]
        public void CaptureErrorInfoTest()
        {
            try { this.MakeException(); }
            catch (Exception ex)
            {
                var info = ex.MakeErrorInfo();

                var text1 = ex.FormatExceptionOutputInfo();
                var text2 = info.FormatErrorInfo();

                Assert.AreEqual(text1, text2);
            }
        }
    }
}
