/* 
    The purpose of this is to error check the data entered into the Create Job wizard.
*/

///////////////////////
// Global Variables //
//////////////////////

// Corresponds to a SET of textboxes (Left and Right reads for instance (two URLs)).
var numTextboxSet = 0;

// This will be dynamically created at runtime to contain a key/value pair corresponding to the rank (step number) and the existence of 
// the step (true if it was checked and false if it was not). This is so we can manage how to go forward and backward dynamically.
var wizardSteps = [];

// The total number of wizard steps that can possibly be seen by a user. For instance, with 3 possible assemblers, that would be 4 + 3 = 7.
var numWizardSteps = 7;

/////////////////////
// Helper Methods //
////////////////////

// Changes the state of the wizard from one step to the next. Next can refer to forward or backward movement.
function ChangeStep(currentStep, nextStep) {
    document.getElementById(currentStep).style.display = "none"; // Hide the contents of the current step.
    document.getElementById(nextStep).style.display = "block"; // Display the contents of the next step.
}

// Clears the text of a div id of location.
function ClearWarning(location) {
    document.getElementById(location).style.display = "none";
}

// Adds a warning to the div with id location with a message.
function AddWarning(location, message) {
    document.getElementById(location).innerHTML = message;
}

// Checks if a string is empty. Returns true if it is null/undefined/empty.
function IsEmpty(location) {
    return (location == null || location === '');
}

// Checks if a value is inclusively between two integets. Returns true if the x-value is, or false if it is not.
function betweenInclusive(x, min, max) {
    return x >= min && x <= max;
}

// Checks if a value is strictly between two integers. Returns true if x-value is, or false if it is not.
function between(x, min, max) {
    return x > min && x < max;
}

// Displays the (variable) assembler steps that depend upon the global AssemblerSteps array to determine which (if any) assembler 
// steps to show next. 
function ChangeAssemblerStep(currentStep, forward) {
    if (typeof (forward) != "boolean")
        throw "Forward must be a boolean (true/false) value.";

    if (typeof (currentStep) != "string")
        throw "CurrentStep must be a string.";

    if (currentStep == "FinalStep") {
        // Don't want to include the final step.
        for (var i = wizardSteps.length - 2; i >= 0; i--) {
            if (wizardSteps[i].value == true) {
                ChangeStep("FinalStep", "Step" + wizardSteps[i].key); // Because of the offset, we must increment here.
                break;
            }
        }
    }

        // Now we determine the other steps.
    else {
        var currentStepNum = parseInt(currentStep.split("p")[1], 10); // Only get the current step number.

        // Move forwards
        if (forward) {
            for (var index = currentStepNum; index < numWizardSteps; index++) {
                if (wizardSteps[index].value) {
                    // Special Case: If we are going to the last step, we must call it predictably.
                    if (index == numWizardSteps - 1) {
                        ChangeStep(currentStep, "FinalStep");
                        break;
                    }

                    else {
                        ChangeStep(currentStep, "Step" + (wizardSteps[index].key));
                        break;
                    }
                }
            }
        }

            // Move backwards
        else {
            var index = -1;

            // For consistency, we need to figure out which step we need the index to start at so we can only consider 
            // elements lower than it.
            for (var i = 0; i < wizardSteps.length - 1; i++) {
                if (wizardSteps[i].key == currentStepNum) {
                    index = i - 1; // We want to go backwards so we subtract 1.
                    break;
                }
            }

            // Find the next step that is available to move back onto.
            for (index; index > 1 ; index--) {
                if (wizardSteps[index].value) {
                    ChangeStep(currentStep, "Step" + (wizardSteps[index].key));
                    break;
                }
            }
        }
    }
}

// Need to fix the styling issue on the textbox so it isn't static. On time crunch so I'm statically assigning values.
function addURLBox(singleURL) {

    if (singleURL) {
        $('#addUrlRow').append(
            "<label id='lab_0' class='control-label col-md-2' style='padding-top: 8px;'> Data Location: </label>"
            + "<div class='row' id='row_0' style='padding-top: 8px;'>"
            + "<div class='col-md-4'><input type='text' id='url_l_0' class='form-control text-box single-line' type='text' placeholder='Single Read URL'></div>"
            + "<div id='DataSourceErrorMsg_0' class='col-md-3 text-danger'></div>"
            + "</div>");

        numTextboxSet++;
    }

    else {
        $('#addUrlRow').append(
            "<label id='lab_" + numTextboxSet + "' class='control-label col-md-2' style='padding-top: 8px;'> Data Location: </label>"
            + "<div class='row' id='row_" + numTextboxSet + "' style='padding-top: 8px;'>"
            + "<div class='col-md-3'><input type='text' id='url_l_" + numTextboxSet + "' class='form-control text-box single-line' type='text' placeholder='Left Read URL'></div>"
            + "<div class='col-md-3'><input type='text' id='url_r_" + numTextboxSet + "' class='form-control text-box single-line' type='text' placeholder='Right Read URL'></div>"
            + "<div id='DataSourceErrorMsg_" + numTextboxSet + "' class='col-md-3 text-danger'></div>"
            + "</div>");

        numTextboxSet++;
    }
}

// Generate the add/remove buttons for the data URL(s).
function addURLButtons() {

    $('#addUrlBtns').append(
        "<div class='row'>"

        + "<div class='col-md-3 col-md-offset-3'>"
        + "<button type='button' id='UrlBtn' onclick='addURLBox()' value='Add Row'>Add URL</button>"
        + "</div>"

        + "<div id='removeUrlBtn' class='col-md-3'>"
        + "<button type='button' id='UrlBtn' onclick='removeURLBox()' value='Remove Row'>Remove URL</button>"
        + "</div>"

        + "</div>")
}

// Removes the url textbox set.
function removeURLBox() {
    if (numTextboxSet > 1) {
        ClearWarning("RemoveURLErrorMsg")
        $("#lab_" + --numTextboxSet).remove();
        $("#row_" + numTextboxSet).remove();
        $("#col_" + numTextboxSet).remove();
        $("#url_" + numTextboxSet).remove();
        $("#DataSourceErrorMsg_" + numTextboxSet).remove();
    }

    else
        AddWarning("RemoveURLErrorMsg", "<br /> You must have at least one data URL. You cannot remove this field!");
}

// This function needs tweeked. It will return valid for something like www.google.comd
function isURL(url) {
    var pattern = new RegExp(/^(?:(?:https?|ftp):\/\/)?(?:www\.)(?:\S+(?::\S*)?@)?(?:(?!10(?:\.\d{1,3}){3})(?!127(?:\.\d{1,3}){3})(?!169\.254(?:\.\d{1,3}){2})(?!192\.168(?:\.\d{1,3}){2})(?!172\.(?:1[6-9]|2\d|3[0-1])(?:\.\d{1,3}){2})(?:[1-9]\d?|1\d\d|2[01]\d|22[0-3])(?:\.(?:1?\d{1,2}|2[0-4]\d|25[0-5])){2}(?:\.(?:[1-9]\d?|1\d\d|2[0-4]\d|25[0-4]))|(?:(?:[a-z\u00a1-\uffff0-9]+-?)*[a-z\u00a1-\uffff0-9]+)(?:\.(?:[a-z\u00a1-\uffff0-9]+-?)*[a-z\u00a1-\uffff0-9]+)*(?:\.(?:[a-z\u00a1-\uffff]{2,})))(?::\d{2,5})?(?:\/[^\s]*)?$/i);

    return pattern.test(url);
}

// Puts all of the URLs together into a single string separated by a comma so we can store it into the model parameter (datasource).
function concatURLs() {

    // Only if there is more than a single textbox do we need to concat the textboxes.
    if (numTextboxSet > 0) {
        var firstSet;
        var leftTextBox
        var rightTextBox

        if (document.getElementById('SequentialReads').checked)
            document.getElementById('DataSource').value = document.getElementById('url_l_0').value;

        else {
            // Define the base case, so we can concat easily.
            leftTextBox = document.getElementById('url_l_0').value;
            rightTextBox = document.getElementById('url_r_0').value;
            firstSet = leftTextBox + "," + rightTextBox;
            document.getElementById('DataSource').value = firstSet;

            // For the rest of the URLs.
            for (var i = 1; i < numTextboxSet; i++) {
                var dataSource = document.getElementById('DataSource').value;

                leftTextBox = document.getElementById('url_l_' + i).value;
                rightTextBox = document.getElementById('url_r_' + i).value;

                document.getElementById('DataSource').value = dataSource + "," + leftTextBox + "," + rightTextBox;
            }
        }
    }
}

// Shows and hides information based on which data type the user checks in step 1.
$(function () {
    // Hide Paired-end reads if checkbox is left unchecked.
    $("#PEReads").click(function () {
        if ($(this).is(':checked')) {
            // Add URL stuff to page.
            addURLBox();
            addURLButtons();

            // Show Paired-End information
            $("#PELength").show();

            // Hide everything else
            $("#JumpReadsGroup").hide();
            $("#SequentialReadsGroup").hide();
        }

        else {
            // Remove the URL stuff from the page (also helps with data integrity).
            $("#addUrlRow").empty();
            $("#addUrlBtns").empty();

            // Hide Paired-End information
            $("#PELength").hide();

            // Show everything else.
            $("#SequentialReadsGroup").show();
            $("#JumpReadsGroup").show();

            numTextboxSet = 0; // Reset the URL box counter.
        }
    });

    // Hide Jump Reads box if checkbox is left unchecked.
    $("#JumpReads").click(function () {
        if ($(this).is(':checked')) {
            // Add URL stuff to page
            addURLBox();
            addURLButtons();

            // Show Jump-Read information
            $("#JumpLength").show();
            $("#MasurcaJumpGroup").show();

            // Hide everything else
            $("#PEReadsGroup").hide();
            $("#SequentialReadsGroup").hide();
        }

        else {
            // Remove the URL stuff from the page (also helps with data integrity).
            $("#addUrlRow").empty();
            $("#addUrlBtns").empty();

            // Hide Jump-Read information
            $("#JumpLength").hide();
            $("#MasurcaJumpGroup").hide();

            // Show everything else
            $("#SequentialReadsGroup").show();
            $("#PEReadsGroup").show();

            numTextboxSet = 0; // Reset the URL box counter.
        }
    });

    // Hide Sequential Reads box if checkbox is left unchecked.
    $("#SequentialReads").click(function () {
        if ($(this).is(':checked')) {
            // Add single URL to page (no buttons)
            addURLBox(true);

            // Show Sequential-Read information
            $("#SequentialReadsGroup").show();
            $("#SequentialLength").show();

            // Hide everything else
            $("#JumpLength").hide();
            $("#PEReadsGroup").hide();
            $("#JumpReadsGroup").hide();

        }

        else {
            // Remove the URL stuff from the page (also helps with data integrity).
            $("#addUrlRow").empty();
            $("#addUrlBtns").empty(); // May not need this, but for completion, I am including it.

            // Hide Sequential-Read information
            $("#SequentialLength").hide();

            // Show everything else
            $("#JumpReadsGroup").show();
            $("#PEReadsGroup").show();
            $("#MasurcaJumpGroup").show();

            numTextboxSet = 0; // Reset the URL box counter.
        }
    });
});

/////////////////////////
// Step Verifications //
////////////////////////

function VerifyStep2() {
    var dataInvalid = false;

    // TODO: Reset ALL warning/error messages prior to running through this each time to make certain that old messages get removed.

    // Check that at least a single data source was entered for the different read types.
    if (numTextboxSet > 0)
    {
        // We have sequential reads.
        if (document.getElementById('SequentialReads').checked)
        {
            if (IsEmpty(document.getElementById("url_l_0").value))
            {
                AddWarning("DataSourceErrorMsg_0", "You need to enter a data source (URL).");
                dataInvalid = true;
            }

            else
                ClearWarning('DataSourceErrorMsg_0');
        }

        // We have other read types.
        else {
            for (var i = 0; i < numTextboxSet; i++) {
                // They added a textbox but didn't enter anything in it.
                if (IsEmpty(document.getElementById("url_l_" + i).value) || IsEmpty(document.getElementById("url_r_" + i).value)) {
                    AddWarning("DataSourceErrorMsg_" + i, "You need to enter a data source (URL).");
                    dataInvalid = true;
                }

                else
                    ClearWarning("DataSourceErrorMsg_" + i);
            }
        }
    }

    // Now check the other information.
    if (document.getElementById("PEReads").checked) {
        var input = document.getElementById("PELengthInput").value;

        // Nothing was entered into the textbox.
        if (IsEmpty(input)) {
            AddWarning("PELengthErrorMsg", "The value you entered was invalid. You must enter a number from 0-100!");
            dataInvalid = true;
        }

        // Make sure the value of the box is between 0 and 100.
        else if (input < 0 || input > 100) {
            AddWarning("PELengthErrorMsg", "The value you entered was invalid. You may only enter numbers from 0-100!");
            dataInvalid = true;
        }

        else 
            ClearWarning("PELengthErrorMsg");
    }

    // Jump read validation is only done if Jump reads is checked.
    else if (document.getElementById("JumpReads").checked) {
        var input = document.getElementById("JumpLengthInput").value;

        if (IsEmpty(input)) {
            AddWarning("JumpLengthErrorMsg", "The value you entered was invalid. You must enter a number from 0-100!");
            dataInvalid = true;
        }

            // Make sure the value of the box is between 0 and 100. (TEST - MAY REMOVE) (WORKS)
        else if (input < 0 || input > 100) {
            AddWarning("JumpLengthErrorMsg", "The value you entered was invalid. You may only enter numbers from 0-100!");
            dataInvalid = true;
        }

        else
            ClearWarning("JumpLengthErrorMsg");
    }

    // Jump read validation is only done if Jump reads is checked.
    else if (document.getElementById("SequentialReads").checked) {
        var input = document.getElementById("SequentialLengthInput").value;

        if (IsEmpty(input)) {
            AddWarning("SequentialLengthErrorMsg", "The value you entered was invalid. You must enter a number from 0-100!");
            dataInvalid = true;
        }

            // Make sure the value of the box is between 0 and 100. (TEST - MAY REMOVE) (WORKS)
        else if (input < 0 || input > 100) {
            AddWarning("SequentialLengthErrorMsg", "The value you entered was invalid. You may only enter numbers from 0-100!");
            dataInvalid = true;
        }

        else
            ClearWarning("SequentialLengthErrorMsg");
    }

        // If no box was checked, then we need to stop them from moving to the next step.
    else {
        AddWarning("checkBoxError", "You must choose at least one type of reads prior to proceeding to the next step.")
        dataInvalid = true;
    }

    // Move to the next step only if all data is valid.
    if (!dataInvalid) {
        ClearWarning("PELengthErrorMsg");
        ClearWarning("JumpLengthErrorMsg");
        ClearWarning("WizardErrors");
        ClearWarning("RemoveURLErrorMsg");
        ClearWarning("PEReadsErrorMsg");
        ClearWarning("JumpReadsErrorMsg");
        ClearWarning("SequentialReadsErrorMsg");
        ClearWarning("SequentialLengthErrorMsg");

        ChangeStep('Step2', 'Step3');
    }

    else
        AddWarning("WizardErrors", "Please Correct the errors prior to proceeding to the next step.")
}

// Choose Assembler Step. Here we re-create the struct that contains the key-value pair corresponding to which steps are 
// present in the runtime iteration. Re-creation of the list is crucial so the user is able to change the assemblers as
// much as the user would like.
function VerifyStep3() {
    var checkedAssemblers = [false, false, false]
    wizardSteps = []; // Clear out the array.

    // Check if an assembler is checked.
    if (document.getElementById('UseMasurca').checked)
        checkedAssemblers[0] = true;

    if (document.getElementById('UseSGA').checked)
        checkedAssemblers[1] = true;

    if (document.getElementById('UseWGS').checked)
        checkedAssemblers[2] = true;

    if (checkedAssemblers.indexOf(true) === -1)
        AddWarning("AssemblerChoiceErrorMsg", "You must at least select a single assembler to run on the data!");

    else
    {
        // Here we dynamically create the wizard step list.
        for (var i = 0; i < numWizardSteps; i++)
        {
            // First three static steps.
            if (i < 3)
            {
                wizardSteps.push(
                    {
                        key: i + 1,
                        value: true
                    });
            }

            // Three assemblers steps.
            else if (i >= 3 && i < numWizardSteps - 1)
            {
                if (checkedAssemblers[i - 3])
                {
                    wizardSteps.push(
                    {
                        key: i + 1,
                        value: true
                    });
                }

                else
                {
                    wizardSteps.push(
                    {
                        key: i + 1,
                        value: false
                    });
                }
            }

            // Final step.
            else
            {
                wizardSteps.push(
                {
                    key: i + 1,
                    value: true
                });
            }
        }

        ClearWarning("AssemblerChoiceErrorMsg");
        ChangeAssemblerStep("Step3", true);
    }
}

// Masurca Assembler Step
function VerifyMasurcaStep() {
    var dataInvalid = false;

    var graphKmerValue = document.getElementById("MasurcaGraphKMerValue").value;
    var kmerValueThreshold = document.getElementById("MasurcaKMerErrorCount").value;
    var cpuThreadNum = document.getElementById("MasurcaThreadNum").value;

    // The graph kmer value entered is not within a valid range.
    if (!IsEmpty(graphKmerValue) && (betweenInclusive(parseInt(graphKmerValue, 10), 25, 101)) == false)
    {
        AddWarning("GraphKmerErrorMsg", "The value you entered was invalid. You may only enter numbers from 25 and 101. Leave blank for auto.");
        dataInvalid = true;
    }

    if(!IsEmpty(kmerValueThreshold) && (betweenInclusive(parseInt(kmerValueThreshold, 10), 1, 2)) == false)
    {
        AddWarning("KMerCountThresholdErrorMsg", "The value you entered was invalid. You may only enter numbers between 1 and 2. Leave blank for auto.");
        dataInvalid = true;
    }

    if (!IsEmpty(cpuThreadNum) && (betweenInclusive(parseInt(cpuThreadNum, 10), 1, 20)) == false)
    {
        AddWarning("ThreadNumErrorMsg", "The value you entered was invalid. You may only enter numbers between 1 and 20.");
        dataInvalid = true;
    }

    if (!dataInvalid) {
        ClearWarning("GraphKmerErrorMsg");
        ClearWarning("KMerCountThresholdErrorMsg");
        ClearWarning("ThreadNumErrorMsg");
        ClearWarning("WizardErrors");

        ChangeAssemblerStep("Step4", true);
    }

    else
        AddWarning("WizardErrors", "Please correct the errors before proceeding!");
}

// SGA Assembler Step
function VerifySgaStep() {
    ChangeAssemblerStep("Step5", true);
}

// WGS Assembler Step
function VerifyWgsStep() {
    ChangeAssemblerStep("Step6", true);
}

// Runs the logic for the final step of the program.
function VerifyFinalStep() {

    // Here we will go through each assembler and figure out which one is checked PRIOR to continuing. Then we will clear out all extraneous data so 
    // needless information isn't passed into the controller. Alternatively, we could handle that in the controller by checking for which assemblers 
    // were checked and simply disregarding any latent information input by the user.
    concatURLs();
}