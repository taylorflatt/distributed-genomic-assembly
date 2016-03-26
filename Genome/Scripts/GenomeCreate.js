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
    var DataURL = document.getElementById("")
    document.getElementById('Step2').style.display = "none";
    document.getElementById('Step3').style.display = "block";
}

function Step3MoveBack(){
    document.getElementById('Step3').style.display = "none";
    document.getElementById('Step2').style.display = "block";
}

function Step3MoveForward() {
    document.getElementById('Step3').style.display = "none";
    document.getElementById('Step4').style.display = "block";
}

function appendURLBox(){
    $('#addUrlBtn').append("<br ><input type='text' id='url_"+ ++x +"' value=''>");
}

function concatURLs() {
    var dataSource = document.getElementById("DataSource").value;
    document.getElementById("DataSource").value = "test"
    for(var i = 2; i <= x; i++ )
    {
        var textboxValue = document.getElementById("url_" + i).value;
        document.getElementById("DataSource").value = dataSource + "," + textboxValue;
        //@Model.DataSource = @Model.DataSource + "," + textBoxValue;
    }
}

$(function ()
{
    // Hide Paired-end reads if checkbox is left unchecked.
    $("#PEReads").click(function (){
        if ($(this).is(':checked')){
            $("#PELength").show();
        }

        else{
            $("#PELength").hide();
        }
    });

    // Hide Jump Reads box if checkbox is left unchecked.
    $("#JumpReads").click(function () {
        if ($(this).is(':checked')) {
            $("#JumpLength").show();
        }

        else {
            $("#JumpLength").hide();
        }
    });
});