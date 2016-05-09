﻿// Global Variables

// Corresponds to a SET of textboxes (Left and Right reads for instance (two URLs)).
var x = 0; 

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
    if (x > 0)
    {
        // We have sequential reads.
        if (x == 1)
        {
            if (document.getElementById("url_l_" + i).value == "")
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

        // We have other read types.
        else
        {
            for (var i = 0; i < x; i++)
            {
                // They added a textbox and didn't enter anything in it.
                if (document.getElementById("url_l_" + i).value == "" ||
                    document.getElementById("url_r_" + i).value == "")
                {
                    document.getElementById("DataSourceErrorMsg_" + i).innerHTML = "You need to enter a data source!";
                    invalidData++;
                    invalidURL++;
                }

                // They added a textbox but entered something (anything) into it.
                else
                {
                    document.getElementById("DataSourceErrorMsg_" + i).innerHTML = "";
                }
            }
        }

        // If there are no errors, make sure the the error message for the first textbox is nulled.
        //if (invalidURL == 0)
        //{
        //    document.getElementById("DataSourceErrorMsg_" + x).innerHTML = "";
        //    invalidURL = 0;
        //}
    }


    // Paired-end read validation is only done if paired-end reads is checked.
    if (document.getElementById("PEReads").checked)
    {
        var input = document.getElementById("PELengthInput").value;

        addURLBox();

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
    else if (document.getElementById("JumpReads").checked)
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

        // Jump read validation is only done if Jump reads is checked.
    else if (document.getElementById("SequentialReads").checked)
    {
        var input = document.getElementById("SequentialLengthInput").value;

        if (input == "") {
            document.getElementById("SequentialLengthErrorMsg").innerHTML = "The value you entered was invalid. You must enter a number from 0-100!";
            invalidData++;
        }

            // Make sure the value of the box is between 0 and 100. (TEST - MAY REMOVE) (WORKS)
        else if (input < 0 || input > 100) {
            document.getElementById("SequentialLengthErrorMsg").innerHTML = "The value you entered was invalid. You may only enter numbers from 0-100!";
            invalidData++;
        }

        else {
            // We can remove the error if they have corrected it but not ALL errors.
            document.getElementById("SequentialLengthErrorMsg").innerHTML = "";
        }
    }

    // If neither box was checked, then we need to stop them from moving to the next step.
    else
    {
        document.getElementById("PEReadsErrorMsg").innerHTML = "You need to at least check one of these boxes!";
        document.getElementById("JumpReadsErrorMsg").innerHTML = "You need to check at least one of these boxes!";
        document.getElementById("SequentialReadsErrorMsg").innerHTML = "You need to check at least one of these boxes!";
        invalidData++;
    }

    if (invalidData == 0)
    {
        document.getElementById("PELengthErrorMsg").innerHTML = "";
        document.getElementById("JumpLengthErrorMsg").innerHTML = "";
        document.getElementById("WizardErrors").innerHTML = "";
        document.getElementById("RemoveURLErrorMsg").innerHTML = "";
        document.getElementById("PEReadsErrorMsg").innerHTML = "";
        document.getElementById("JumpReadsErrorMsg").innerHTML = "";
        document.getElementById("SequentialReadsErrorMsg").innerHTML = "";
        document.getElementById("SequentialLengthErrorMsg").innerHTML = "";

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
function addURLBox(singleURL) {
    //document.getElementById("RemoveURLErrorMsg").innerHTML = "";

    if (singleURL)
    {
        $('#addUrlRow').append(
            "<label id='lab_" + x + "' class='control-label col-md-3' style='padding-top: 8px;'> Data Location (URL): </label>"
            + "<div class='row' id='row_" + x + "' style='padding-top: 8px;'>"
            + "<div class='col-md-4'><input type='text' id='url_l_" + x + "' class='form-control text-box single-line' type='text' placeholder='Single Read URL'></div>"
            + "<div id='DataSourceErrorMsg_" + x + "' class='col-md-4 text-danger'></div>"
            + "</div>");

        x++;
    }

    else
    {
        $('#addUrlRow').append(
            "<label id='lab_" + x + "' class='control-label col-md-3' style='padding-top: 8px;'> Data Location (URL): </label>"
            + "<div class='row' id='row_" + x + "' style='padding-top: 8px;'>"
            + "<div class='col-md-4'><input type='text' id='url_l_" + x + "' class='form-control text-box single-line' type='text' placeholder='Left Read URL'></div>"
            + "<div class='col-md-4'><input type='text' id='url_r_" + x + "' class='form-control text-box single-line' type='text' placeholder='Right Read URL'></div>"
            + "<div id='DataSourceErrorMsg_" + x + "' class='col-md-4 text-danger'></div>"
            + "</div>");

        x++;
    }
}

// Generate the add/remove buttons for the data URL(s).
function addURLButtons() {

    $('#addUrlBtns').append(
        "<div class='row'>"

        +   "<div class='col-md-3 col-md-offset-5'>"
        +       "<button type='button' id='UrlBtn' onclick='addURLBox()' value='Add Row'>Add URL</button>"
        +   "</div>"

        +   "<div id='removeUrlBtn' class='col-md-2'>"
        +       "<button type='button' id='UrlBtn' onclick='removeURLBox()' value='Remove Row'>Remove URL</button>"
        +   "</div>"

        + "</div>")
}

function removeURLBox() {
    if (x > 1)
    {
        document.getElementById("RemoveURLErrorMsg").innerHTML = "";
        $("#lab_" + --x).remove();
        $("#row_" + x).remove();
        $("#col_" + x).remove();
        $("#url_" + x).remove();
        $("#DataSourceErrorMsg_" + x).remove();
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
    if (x > 1) {
        for (var i = 0; i < x; i++) {
            var dataSource = document.getElementById("url_0").value; // Get first textbox.
            var textboxValue = document.getElementById("url_" + i).value; // Get second text box.
            document.getElementById("url_0").value = dataSource + "," + textboxValue; // Combine their values delineated by a comma.
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
            $("#MasurcaPEGroup").show();
        }

        else
        {
            $("#addUrlRow").empty(); // Delete the URL boxes from the page.
            $("#addUrlBtns").empty(); // Delete the URL buttons from the page.
            $("#PELength").hide();
            $("#SequentialReadsGroup").show();
            $("#JumpReadsGroup").show();
            $("#MasurcaPEGroup").hide();

            x = 0; // Reset the URL box counter.
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

            x = 0; // Reset the URL box counter.
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
            $("#MasurcaPEGroup").hide();
        }

        else
        {
            $("#addUrlRow").empty(); // Delete the URL boxes from the page.
            $("#addUrlBtns").empty(); // Delete the URL buttons from the page (may not need this).
            $("#SequentialLength").hide();
            $("#JumpReadsGroup").show();
            $("#PEReadsGroup").show();
            $("#MasurcaJumpGroup").show();

            x = 0; // Reset the URL box counter.
        }
    });
});