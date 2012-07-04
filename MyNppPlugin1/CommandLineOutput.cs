using System;
using System.Collections.Generic;
using System.Text;

namespace NppRunPerl {
    class CommandLineOutput {
        private string output;
        private string error;

        public CommandLineOutput(string output, string error) {
            this.output = output;
            this.error = error;
        }

        public override string ToString() {
            if (isEmpty(error)) {
                if (isEmpty(output)) {
                    return "[no output]";
                } else {
                    return output;
                }
            } else {
                if (isEmpty(output)) {
                    return error;
                } else {
                    return "Error:\n" + error + "\n\nOutput:\n" + output;
                }
            }
        }

        public bool ErrorsPresent() {
            return error.Length > 0;
        }

        private bool isEmpty(string str) {
            return (str == null || str.Length <= 0);
        }

        public string Output {
            get { return output; }
        }
        public string Error {
            get { return error; }
        }

    }
}
