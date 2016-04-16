// Global Variables

// Used to denote the URL textboxes
var x = 1; 

// Used to manage the steps that need to be displayed for the assemblers.
var MasurcaStep = 0;
var SGAStep = 0;
var WGSStep = 0;
var AssemblerSteps = [MasurcaStep, SGAStep, WGSStep];

function Step1MoveForward() {
    document.getElementById('Step1').style.display = "none";
    document.getElementById('Step2').style.display = "block";
}

function Step2MoveBack(){
    document.getElementById('Step2').style.display = "none";
    document.getElementById('Step1').style.display = "block";
}

function Step2MoveForward() {
    var invalidData = 0;
    var invalidURL = 0; // Determine if we still have an invalid URL.

    if (document.getElementById("url_0").value == "")
    {
        document.getElementById("DataSourceErrorMsg_0").innerHTML = "You need to enter a data source!";
        invalidData++;
        invalidURL++;
    }

    else
    {
        document.getElementById("DataSourceErrorMsg_0").innerHTML = "";
    }

    // Only if there are more text boxes entered.
    if (x > 1)
    {
        for (var i = 2; i <= x; i++)
        {
            if (document.getElementById("url_" + i).value == "")
            {
                document.getElementById("DataSourceErrorMsg_" + i).innerHTML = "You need to enter a data source!";
                invalidData++;
                invalidURL++;
            }

            else
            {
                document.getElementById("DataSourceErrorMsg_" + i).innerHTML = "";
            }
        }

        if (invalidURL == 0)
        {
            document.getElementById("DataSourceErrorMsg_" + x).innerHTML = "";
            invalidURL = 0;
        }
    }


    // Paired-end read validation is only done if paired-end reads is checked.
    if (document.getElementById("PEReads").checked)
    {
        var input = document.getElementById("PELengthInput").value;

        if (input == "")
        {
            document.getElementById("PELengthErrorMsg").innerHTML = "The value you entered was invalid. You must enter a number from 0-100!";
            invalidData++;
        }

        // Make sure the value of the box is between 0 and 100. (TEST - MAY REMOVE) (WORKS)
        else if(input < 0 || input > 100)
        {
            document.getElementById("PELengthErrorMsg").innerHTML = "The value you entered was invalid. You may only enter numbers from 0-100!";
            invalidData++;
        }

        else
        {
            // We can remove the error if they have corrected it but not ALL errors.
            document.getElementById("PELengthErrorMsg").innerHTML = "";
        }
    }

    // Jump read validation is only done if Jump reads is checked.
    if (document.getElementById("JumpReads").checked)
    {
        var input = document.getElementById("JumpLengthInput").value;

        if (input == "")
        {
            document.getElementById("JumpLengthErrorMsg").innerHTML = "The value you entered was invalid. You must enter a number from 0-100!";
            invalidData++;
        }

        // Make sure the value of the box is between 0 and 100. (TEST - MAY REMOVE) (WORKS)
        else if (input < 0 || input > 100)
        {
            document.getElementById("JumpLengthErrorMsg").innerHTML = "The value you entered was invalid. You may only enter numbers from 0-100!";
            invalidData++;
        }

        else
        {
            // We can remove the error if they have corrected it but not ALL errors.
            document.getElementById("JumpLengthErrorMsg").innerHTML = "";
        }
    }


    if (invalidData == 0)
    {
        document.getElementById("PELengthErrorMsg").innerHTML = "";
        document.getElementById("JumpLengthErrorMsg").innerHTML = "";
        document.getElementById("WizardErrors").innerHTML = "";

        // Move to the next step.
        document.getElementById('Step2').style.display = "none";
        document.getElementById('Step3').style.display = "block";
    }

    else
    {
        // Write an error message at the top of the page indicating that one or more fields are incorrect.
        document.getElementById("WizardErrors").innerHTML = "Please correct the errors before proceeding!";
    }
}

function Step3MoveBack() {

    document.getElementById('Step3').style.display = "none";
    document.getElementById('Step2').style.display = "block";
}

// Choose Assembler Step
function Step3MoveForward() {

    // Check if an assembler is checked.
    if (document.getElementById('UseMasurca').checked) {
        AssemblerSteps[0] = 1;
    }

    if (document.getElementById('UseSGA').checked) {
        AssemblerSteps[1] = 1;
    }

    if (document.getElementById('UseWGS').checked) {
        AssemblerSteps[2] = 1;
    }

    // Display the correct first step.
    if (AssemblerSteps[0] == 1) {
        document.getElementById('Step3').style.display = "none";
        document.getElementById('Step4').style.display = "block"; // Masurca Assembler step

        document.getElementById("AssemblerChoiceErrorMsg").innerHTML = "";
    }

    else if (AssemblerSteps[1] == 1) {
        document.getElementById('Step3').style.display = "none";
        document.getElementById('Step5').style.display = "block"; // SGA Assembler step

        document.getElementById("AssemblerChoiceErrorMsg").innerHTML = "";
    }

    else if (AssemblerSteps[2] == 1) {
        document.getElementById('Step3').style.display = "none";
        document.getElementById('Step6').style.display = "block"; // WGS Assembler step

        document.getElementById("AssemblerChoiceErrorMsg").innerHTML = "";
    }

    // They didn't select at least a single assembler to run their data through.
    else
    {
        document.getElementById("AssemblerChoiceErrorMsg").innerHTML = "You must at least select a single assembler to run on the data!";
    }
}

// Masurca Assembler Step
function Step4MoveForward() {
    var invalidData = 0;
    var graphKmerValue = document.getElementById("MasurcaGraphKMerValue").value;
    var kmerValueThreshold = document.getElementById("MasurcaKMerErrorCount").value;
    var cpuThreadNum = document.getElementById("MasurcaThreadNum").value;


    if (graphKmerValue != "" && (betweenInclusive(parseInt(graphKmerValue, 10), 25, 101)) == false)
    {
        document.getElementById("GraphKmerErrorMsg").innerHTML = "The value you entered was invalid. You may only enter numbers from 25 and 101. Leave blank for auto.";
        invalidData++;
    }

    if(kmerValueThreshold != "" && (betweenInclusive(parseInt(kmerValueThreshold, 10), 1, 2)) == false)
    {
        document.getElementById("KMerCountThresholdErrorMsg").innerHTML = "The value you entered was invalid. You may only enter numbers between 1 and 2. Leave blank for auto.";
        invalidData++;
    }

    if (cpuThreadNum != "" && (betweenInclusive(parseInt(cpuThreadNum, 10), 1, 20)) == false)
    {
        document.getElementById("ThreadNumErrorMsg").innerHTML = "The value you entered was invalid. You may only enter numbers between 1 and 20.";
        invalidData++;
    }

    if (invalidData == 0) {
        document.getElementById("GraphKmerErrorMsg").innerHTML = "";
        document.getElementById("KMerCountThresholdErrorMsg").innerHTML = "";
        document.getElementById("ThreadNumErrorMsg").innerHTML = "";
        document.getElementById("WizardErrors").innerHTML = "";

        if (AssemblerSteps[1] == 1) {
            document.getElementById('Step4').style.display = "none";
            document.getElementById('Step5').style.display = "block"; // SGA Assembler step
        }

        else if (AssemblerSteps[2] == 1) {
            document.getElementById('Step4').style.display = "none";
            document.getElementById('Step6').style.display = "block"; // WGS Assembler step
        }

        else {
            document.getElementById('Step4').style.display = "none";
            document.getElementById('FinalStep').style.display = "block"; // Final step
        }
    }

    else {
        document.getElementById("WizardErrors").innerHTML = "Please correct the errors before proceeding!";
    }
}

function Step4MoveBackward() {
    document.getElementById('Step4').style.display = "none";
    document.getElementById('Step3').style.display = "block";
}

function FinalStepMoveBackward() {
    document.getElementById('FinalStep').style.display = "none";
    document.getElementById('Step4').style.display = "block";
}

function DisplayFinalStepError() {
    document.getElementById('Step1').style.display = "none";
    document.getElementById('FinalStep').style.display = "block";
}

// Need to fix the styling issue on the textbox so it isn't static. On time crunch so I'm statically assigning values.
function addURLBox() {
    document.getElementById("RemoveURLErrorMsg").innerHTML = "";
    $('#addUrlBtn').append("<label id='lab_" + ++x + "' class='control-label col-md-3' style='padding-top: 8px;'> Data Location (URL): </label><div class='row' id='row_" + x + "' style='padding-top: 8px;'><div class='col-md-4' id='col_" + x + "'><input type='text' id='url_" + x + "' value='' class='form-control text-box single-line' style='width: 229.984px;' type='text'></div><div id='DataSourceErrorMsg_" + x + "' class='col-md-4 text-danger'></div></div>");
}

function removeURLBox() {
    if (x > 1)
    {
        document.getElementById("RemoveURLErrorMsg").innerHTML = "";
        $("#lab_" + x).remove();
        $("#row_" + x).remove();
        $("#col_" + x).remove();
        $("#url_" + x).remove();
        $("#DataSourceErrorMsg_" + x).remove();
        x--;
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

    for(var i = 2; i <= x; i++ )
    {
        var dataSource = document.getElementById("DataSource").value;
        var textboxValue = document.getElementById("url_" + i).value;
        document.getElementById("DataSource").value = dataSource + "," + textboxValue;
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
            $("#PELength").show();
            $("#JumpReadsGroup").hide();
            $("#MasurcaPEGroup").show();
        }

        else{
            $("#PELength").hide();
            $("#JumpReadsGroup").show();
            $("#MasurcaPEGroup").hide();
        }
    });

    // Hide Jump Reads box if checkbox is left unchecked.
    $("#JumpReads").click(function ()
    {
        if ($(this).is(':checked'))
        {
            $("#JumpLength").show();
            $("#PEReadsGroup").hide();
            $("#MasurcaJumpGroup").show();
        }

        else {
            $("#JumpLength").hide();
            $("#PEReadsGroup").show();
            $("#MasurcaJumpGroup").hide();
        }
    });
});