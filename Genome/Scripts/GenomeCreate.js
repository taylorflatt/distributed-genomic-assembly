/* 
    The purpose of this is to error check the data entered into the Create Job wizard.
*/




// TODO: On the FINAL step, we need to know which assemblers the user wants to use. That means, we need to grab the checked boxes 
// AGAIN and only care about the data under those assemblers. Or find some other method/work around to handle moving back and forth.




// Global Variables

// Corresponds to a SET of textboxes (Left and Right reads for instance (two URLs)).
var numTextboxSet = 0;

// Corresponds to the number of assemblers deployed.
var numAssemblers = 3;

// This will be dynamically created at runtime to contain a key/value pair corresponding to the rank (step number) and the existence of 
// the step (true if it was checked and false if it was not). This is so we can manage how to go forward and backward dynamically.
var wizardSteps = [];

var numWizardSteps = 7;

// Changes the state of the wizard from one step to the next. Next can refer to forward or backward movement.
function ChangeStep(currentStep, nextStep) {
    document.getElementById(currentStep).style.display = "none"; // Hide the contents of the current step.
    document.getElementById(nextStep).style.display = "block"; // Display the contents of the next step.
}

function ClearWarning(location) {
    document.getElementById(location).style.display = "none";
}

function AddWarning(location, message) {
    document.getElementById(location).innerHTML = message;
}

function IsEmpty(location) {
    return (location == null || location === '');
}

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

// Displays the (variable) assembler steps that depend upon the global AssemblerSteps array to determine which (if any) assembler 
// steps to show next. 
function DisplayAssemblerStep(currentStep, forward)
{
    if (typeof (forward) != "boolean")
        throw "Forward must be a boolean (true/false) value.";

    if (typeof (currentStep) != "string")
        throw "CurrentStep must be a string.";

    // Special Case: We are on the last step and thus do not have a step number.
    if (currentStep == "FinalStep")
    {
        // We want to find the last step that was checked. We then add 3 for the 3 steps that come before it and 1 more because of the zero index so + 4.
        var previousAssemblerStep = assemblerSteps.lastIndexOf(true) + 4;

        ChangeStep("FinalStep", "Step" + previousAssemblerStep)
    }

    // Now we determine the other steps.
    else
    {
        var currentStepNum = parseInt(currentStep.split("p")[1], 10); // Only get the current step number.

        // Move to a next assembler step.
        if (forward) {
            // Note: Keep in mind that currentStepNum is based on a 1 starting index and wizardSteps is based on a zero index.
            for (var index = currentStepNum; index < numWizardSteps; index++)
            {
                if (wizardSteps[index].value)
                {
                    // Special Case: If we are going to the last step, we must call it predictably.
                    if (index = numWizardSteps - 1)
                    {
                        ChangeStep(currentStep, "FinalStep");
                        break;
                    }

                    else
                    {
                        ChangeStep(currentStep, "Step" + (wizardSteps[index].key + 1)); // Increment by 1 because of the differing indices.
                        break;
                    }
                }
            }
        }

        else
        {
            for(var index = currentStepNum; index >= 0; index--)
            {
                if(wizardSteps[index - 1].value)
                {
                    ChangeStep(currentStep, "Step" + (wizardSteps[index - 1].key));
                    break;
                }
            }
        }
    }
}

// Choose Assembler Step
function VerifyStep3() {
    numAssemblerSteps = 3;
    var checkedAssemblers = [false,false,false]

    // Check if an assembler is checked.
    if (document.getElementById('UseMasurca').checked)
        checkedAssemblers[0] = true;

    if (document.getElementById('UseSGA').checked)
        checkedAssemblers[1] = true;

    if (document.getElementById('UseWGS').checked)
        checkedAssemblers[2] = true;

    if (checkedAssemblers.indexOf(true) === -1)
        AddWarning("AssemblerChoiceErrorMsg", "You must at least select a single assembler to run on the data!");

    // TODO: MOVE FIRST THREE STEPS OUTSIDE AND MAKE THEM DEFAULT PART OF THE ARRAY.
    else
    {
        // Here we dynamically create the wizard step list.
        for (var i = 0; i < numWizardSteps; i++)     // numAssemblers + 4 will always be the total number of steps.
        {
            // First three static steps.
            if (i < 3)
            {
                wizardSteps.push(
                    {
                        key: i,
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
                        key: i,
                        value: true
                    });
                }

                else
                {
                    wizardSteps.push(
                    {
                        key: i,
                        value: false
                    });
                }
            }

            // Final step.
            else
            {
                wizardSteps.push(
                {
                    key: i,
                    value: true
                });
            }
        }

        ClearWarning("AssemblerChoiceErrorMsg");
        DisplayAssemblerStep("Step3", true);
    }
}

function VerifyTestStep()
{
    DisplayAssemblerStep("Step5", true);
}

function VerifyTestStep2() {
    DisplayAssemblerStep("Step6", true);
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

        DisplayAssemblerStep("Step4", true);
    }

    else
        AddWarning("WizardErrors", "Please correct the errors before proceeding!");
}

function Step4MoveBackward() {
    document.getElementById('Step4').style.display = "none";
    document.getElementById('Step3').style.display = "block";
}

function FinalStepMoveBackward() {
    document.getElementById('FinalStep').style.display = "none";
    document.getElementById('Step4').style.display = "block";
}

// Need to fix the styling issue on the textbox so it isn't static. On time crunch so I'm statically assigning values.
function addURLBox(singleURL) {
    //document.getElementById("RemoveURLErrorMsg").innerHTML = "";

    if (singleURL)
    {
        $('#addUrlRow').append(
            "<label id='lab_0' class='control-label col-md-2' style='padding-top: 8px;'> Data Location: </label>"
            + "<div class='row' id='row_0' style='padding-top: 8px;'>"
            + "<div class='col-md-4'><input type='text' id='url_l_0' class='form-control text-box single-line' type='text' placeholder='Single Read URL'></div>"
            + "<div id='DataSourceErrorMsg_0' class='col-md-3 text-danger'></div>"
            + "</div>");

        numTextboxSet++;
    }

    else
    {
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

        +   "<div class='col-md-3 col-md-offset-3'>"
        +       "<button type='button' id='UrlBtn' onclick='addURLBox()' value='Add Row'>Add URL</button>"
        +   "</div>"

        +   "<div id='removeUrlBtn' class='col-md-3'>"
        +       "<button type='button' id='UrlBtn' onclick='removeURLBox()' value='Remove Row'>Remove URL</button>"
        +   "</div>"

        + "</div>")
}

function removeURLBox() {
    if (numTextboxSet > 1)
    {
        document.getElementById("RemoveURLErrorMsg").innerHTML = "";
        $("#lab_" + --numTextboxSet).remove();
        $("#row_" + numTextboxSet).remove();
        $("#col_" + numTextboxSet).remove();
        $("#url_" + numTextboxSet).remove();
        $("#DataSourceErrorMsg_" + numTextboxSet).remove();
    }

    else
    {
        document.getElementById("RemoveURLErrorMsg").innerHTML = "<br /> You must have at least one data URL. You cannot remove this field!";
    }
}

// This function needs tweeked. It will return valid for something like www.google.comd
function isURL(url) {
    var pattern = new RegExp(/^(?:(?:https?|ftp):\/\/)?(?:www\.)(?:\S+(?::\S*)?@)?(?:(?!10(?:\.\d{1,3}){3})(?!127(?:\.\d{1,3}){3})(?!169\.254(?:\.\d{1,3}){2})(?!192\.168(?:\.\d{1,3}){2})(?!172\.(?:1[6-9]|2\d|3[0-1])(?:\.\d{1,3}){2})(?:[1-9]\d?|1\d\d|2[01]\d|22[0-3])(?:\.(?:1?\d{1,2}|2[0-4]\d|25[0-5])){2}(?:\.(?:[1-9]\d?|1\d\d|2[0-4]\d|25[0-4]))|(?:(?:[a-z\u00a1-\uffff0-9]+-?)*[a-z\u00a1-\uffff0-9]+)(?:\.(?:[a-z\u00a1-\uffff0-9]+-?)*[a-z\u00a1-\uffff0-9]+)*(?:\.(?:[a-z\u00a1-\uffff]{2,})))(?::\d{2,5})?(?:\/[^\s]*)?$/i);

    return pattern.test(url);
}

function concatURLs() {

    // Only if there is more than a single textbox do we need to concat the textboxes.
    if (numTextboxSet > 0)
    {
        var firstSet;
        var leftTextBox
        var rightTextBox

        if (document.getElementById('SequentialReads').checked)
        {
            document.getElementById('DataSource').value = document.getElementById('url_l_0').value;
        }

        else
        {
            // Define the base case, so we can concat easily.
            leftTextBox = document.getElementById('url_l_0').value;
            rightTextBox = document.getElementById('url_r_0').value;
            firstSet = leftTextBox + "," + rightTextBox;
            document.getElementById('DataSource').value = firstSet;

            // For the rest of the URLs.
            for (var i = 1; i < numTextboxSet; i++)
            {
                var dataSource = document.getElementById('DataSource').value;

                leftTextBox = document.getElementById('url_l_' + i).value;
                rightTextBox = document.getElementById('url_r_' + i).value;

                document.getElementById('DataSource').value = dataSource + "," + leftTextBox + "," + rightTextBox;
            }
        }


    }
}

function betweenInclusive(x, min, max) {
    return x >= min && x <= max;
}

function between(x, min, max) {
    return x > min && x < max;
}

$(function ()
{
    // Hide Paired-end reads if checkbox is left unchecked.
    $("#PEReads").click(function ()
    {
        if ($(this).is(':checked'))
        {
            addURLBox(); // Add URL boxes to the page.
            addURLButtons(); // Add URL buttons to the page.
            $("#PELength").show();
            $("#JumpReadsGroup").hide();
            $("#SequentialReadsGroup").hide();
        }

        else
        {
            $("#addUrlRow").empty(); // Delete the URL boxes from the page.
            $("#addUrlBtns").empty(); // Delete the URL buttons from the page.
            $("#PELength").hide();
            $("#SequentialReadsGroup").show();
            $("#JumpReadsGroup").show();

            numTextboxSet = 0; // Reset the URL box counter.
        }
    });

    // Hide Jump Reads box if checkbox is left unchecked.
    $("#JumpReads").click(function ()
    {
        if ($(this).is(':checked'))
        {
            addURLBox(); // Add URL boxes to the page.
            addURLButtons(); // Add URL buttons to the page.
            $("#JumpLength").show();
            $("#PEReadsGroup").hide();
            $("#SequentialReadsGroup").hide();
            $("#MasurcaJumpGroup").show();
        }

        else
        {
            $("#addUrlRow").empty(); // Delete the URL boxes from the page.
            $("#addUrlBtns").empty(); // Delete the URL buttons from the page.
            $("#JumpLength").hide();
            $("#SequentialReadsGroup").show();
            $("#PEReadsGroup").show();
            $("#MasurcaJumpGroup").hide();

            numTextboxSet = 0; // Reset the URL box counter.
        }
    });

    // Hide Jump Reads box if checkbox is left unchecked.
    $("#SequentialReads").click(function () {
        if ($(this).is(':checked'))
        {
            addURLBox(true); // Add URL boxes to the page.
            $("#JumpLength").hide();
            $("#PEReadsGroup").hide();
            $("#JumpReadsGroup").hide();
            $("#SequentialReadsGroup").show();
            $("#SequentialLength").show();
        }

        else
        {
            $("#addUrlRow").empty(); // Delete the URL boxes from the page.
            $("#addUrlBtns").empty(); // Delete the URL buttons from the page (may not need this).
            $("#SequentialLength").hide();
            $("#JumpReadsGroup").show();
            $("#PEReadsGroup").show();
            $("#MasurcaJumpGroup").show();

            numTextboxSet = 0; // Reset the URL box counter.
        }
    });
});