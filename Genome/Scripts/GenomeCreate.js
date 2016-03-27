var x = 1;

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

    if (document.getElementById("DataSource").value == "")
    {
        document.getElementById("DataSourceErrorMsg").innerHTML = "You need to enter a data source!";
        invalidData++;
        invalidURL++;
    }

    // Only if there are more text boxes entered.
    else if (x > 1)
    {
        for (var i = 2; i <= x; i++)
        {
            if (document.getElementById("url_" + i).value == "")
            {
                document.getElementById("DataSourceErrorMsg").innerHTML = "You need to enter a data source!";
                invalidData++;
                invalidURL++;
            }
        }

        if (invalidURL == 0)
        {
            document.getElementById("DataSourceErrorMsg").innerHTML = "";
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
        // Reset the error warnings (Potentially unnecessary, just precautionary).
        document.getElementById("DataSourceErrorMsg").innerHTML = "";
        document.getElementById("PELengthErrorMsg").innerHTML = "";
        document.getElementById("JumpLengthErrorMsg").innerHTML = "";
        document.getElementById("WizardErrors").innerHTML = "";
        document.getElementById("DataSourceErrorMsg").innerHTML = "";

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

function Step3MoveForward() {
    var invalidData = 0;
    var graphKmerValue = document.getElementById("MasurcaGraphKMerValue").value;
    var kmerValueThreshold = document.getElementById("MasurcaKMerErrorCount").value;
    var cpuThreadNum = document.getElementById("MasurcaThreadNum").value;

    if (graphKmerValue != "" && (graphKmerValue < 25 || graphKmerValue > 101))
    {
        document.getElementById("GraphKmerErrorMsg").innerHTML = "The value you entered was invalid. You may only enter numbers from 25 and 101. Leave blank for auto.";
        invalidData++;
    }

    if (kmerValueThreshold != "" && (kmerValueThreshold != 1 || kmerValueThreshold != 2))
    {
        document.getElementById("KMerCountThresholdErrorMsg").innerHTML = "The value you entered was invalid. You may only enter numbers between 1 and 2. Leave blank for auto.";
        invalidData++;
    }

    if (cpuThreadNum != "" && (cpuThreadNum < 1 || cpuThreadNum > 20))
    {
        document.getElementById("ThreadNumErrorMsg").innerHTML = "The value you entered was invalid. You may only enter numbers between 1 and 20.";
        invalidData++;
    }

    if (invalidData == 0)
    {
        document.getElementById("GraphKmerErrorMsg").innerHTML = "";
        document.getElementById("KMerCountThresholdErrorMsg").innerHTML = "";
        document.getElementById("ThreadNumErrorMsg").innerHTML = "";
        document.getElementById("WizardErrors").innerHTML = "";

        document.getElementById('Step3').style.display = "none";
        document.getElementById('Step4').style.display = "block";
    }

    else
    {
        document.getElementById("WizardErrors").innerHTML = "Please correct the errors before proceeding!";
    }
}

function Step4MoveBackward() {
    document.getElementById('Step4').style.display = "none";
    document.getElementById('Step3').style.display = "block";
}

function addURLBox(){
    $('#addUrlBtn').append("<br id='br_" + ++x + "'><input type='text' id='url_" + x + "' value=''>");
}

function removeURLBox() {
    if (x > 1)
    {
        $("#br_" + x).remove();
        $("#url_" + x).remove();
        x--;
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

$(function ()
{
    // Hide Paired-end reads if checkbox is left unchecked.
    $("#PEReads").click(function ()
    {
        if ($(this).is(':checked'))
        {
            $("#PELength").show();
        }

        else{
            $("#PELength").hide();
        }
    });

    // Hide Jump Reads box if checkbox is left unchecked.
    $("#JumpReads").click(function ()
    {
        if ($(this).is(':checked'))
        {
            $("#JumpLength").show();
        }

        else {
            $("#JumpLength").hide();
        }
    });
});