$(function(){

 let PATH = $.cookie("currenttree"), NEEDSREFRESH = false;
 if(!PATH)
  PATH="/";
 LoadTree(PATH);

 function htmlEncode(value){
  return $('<textarea/>').text(value).html();
 }

 $("#btnGotoRoot").click(function(){
  PATH="/";
  LoadTree("/");
 });

 // TO DO
 // HIER HIER
 // - (DOwnloaden)btnFromServer
 // - Onthouden laatste map met cookie
 // - verversen na upload to server
 // Nieuwe versie:
 // Onthoud ergens (in shelve) waar je mee bezig was en restore browser window met progress dialoog
// Diti kan door te kijken of de thread nog runt
 // met bijv. progressidaloog
 // Sla op in shleve welke mappen van de server hij wil syncen (Selectieve synchronisatiwe)

 /* Files list (tree) */

  function LoadTree(path) {
   if(path==="/") $("#btnDeleteFolder").prop('disabled', true); else $("#btnDeleteFolder").prop('disabled', false);
   $("#tree").html('<div class="wait"><div class="wait lds-hourglass"></div></div>');
   $.cookie("currenttree", path, { expires: 200 });
   var U="/?cmd=ls&path="+encodeURIComponent(path);
   console.log("Query "+U);
   $.ajax({url:U}).done(function(d){
    checkForErrors("Error requesting directory listing");
    var htmlf="",htmld="";
    if(path!="/") {
     htmld += "<a href='#' title='Move up' data-dir='..'>";
     htmld += "<img src='/pix/flder.png' />"
     htmld += "<span class='fname'>..</span>";
     htmld += "<span class='fsize'>&nbsp;</span>";
     htmld += "<span class='fdate'>&nbsp;</span>";
     htmld += "</a>";
    }
    var Lines = d.split(/\r?\n/);
    for(var i=0; i<Lines.length; i++) {
     FileInfo = Lines[i].split("///");
     if(FileInfo.length==4) {
      if(FileInfo[0]=='f') {
       htmlf += "<a href='#' title='Download "+FileInfo[2].replace(/'/g,"&apos;")+"' data-file='"+FileInfo[2].replace(/'/g,"&apos;")+"'>";
       htmlf += "<img src='/pix/file.png' />"
       htmlf += "<span class='fname'>"+FileInfo[2]+"</span>";
       htmlf += "<span class='fsize'>"+FileInfo[1]+"</span>";
       htmlf += "<span class='fdate'>"+FileInfo[3]+"</span>";
       htmlf += "</a>";
      }
      else { // d (folder
       htmld += "<a href='#' title='"+FileInfo[2].replace(/'/g,"&apos;")+"' data-dir='"+FileInfo[2].replace(/'/g,"&apos;")+"'>";
       htmld += "<img src='/pix/flder.png' />"
       htmld += "<span class='fname'>"+FileInfo[2]+"</span>";
       htmld += "<span class='fsize'> </span>";
       htmld += "<span class='fdate'>"+FileInfo[3]+"</span>";
       htmld += "</a>";
      }
     }
     else if(FileInfo.length!=0 && Lines[i].length>=0) console.error("Syncie Error: unkown file info: "+Lines[i]+".");
    }
    $("#tree").html(htmld+htmlf);
    SetEvents();
    $(".path").text(path);
   });
  }

  function SetEvents() {
   // Show number of files in folder
   $("a[data-dir]").each(function(){
     $.ajax({url:"/?cmd=numfilesinfolder&folder="+encodeURIComponent(PATH+$(this).attr("data-dir")), context:$(this)}).done(function(d){
      $(".fsize",this).html(d);
     });
   });
 
   
   $("a[data-dir]").click(function(){
    var dir = $(this).attr("data-dir");
    if(dir==="..") {
     if(!PATH.endsWith("/")) PATH+="/";
     PATH=PATH.split( '/' ).slice(0,-2).join( '/' )+"/";
    }
    else
     PATH=PATH+dir+"/";

    LoadTree(PATH);
   (this)});

   $("a[data-file]").click(function(){
    file = $(this).attr("data-file");
    location.href="/?cmd=downloadfile&remotefile="+encodeURIComponent(PATH+file);
   });
  }
  
  $("#btnSyncResult").click(function(){
   var r ="";
   function AddInfo(array) {   
    if(array.length<=0) r+="--- none ---<br />"; 
    else {
     r += "<div class='tt'>";
     for(var i=0; i<array.length; i++) {r += array[i]+"<br />"; }
     r += "</div>";
    }
   }
   $("#dialogResult").show();
   $.ajax({url:"/?cmd=getlastrsyncdetails"}).done(function(data){
    var a = data.split("$$$|$$$");
    r+="<b>Files downloaded</b></br />";AddInfo(eval(a[0]));
    r+="<b>Files uploaded</b></br />"; AddInfo(eval(a[3]));        
    r+="<b>Folders created on server</b></br />"; AddInfo(eval(a[2]));        
    r+="<b>Folders created on client</b></br />"; AddInfo(eval(a[1]));
    $("#dialogResultText").html(r);
   });
  });
  
  $("#btnConfig").click(function(){
   location.href="config.html";   
  });

  $("#btnFromServer").click(function(){
   NEEDSREFRESH=false;
   showProgress("Synchronize from server (download)","Initializing...");
   $.ajax({url:'/?cmd=download&path='+encodeURIComponent($("#path").text())}).done(function(){
    goProgress();
   });
  });

  $("#btnToServer").click(function(){
   NEEDSREFRESH=true;
   showProgress("Synchronize to server (upload)","Initializing...");
   $.ajax({url:'/?cmd=upload&path='+encodeURIComponent($("#path").text())}).done(function(){
    goProgress();
   });
  });

  $("#btnDeleteFolder").click(function(){
   showConfirm("Delete folder "+$("#path").text()+" from the server?");
   $("#btnConfirmYes").click(function(){
    hideConfirm();
    var path = $("#path").text();
    // Set path to go to after deleting the folder
    var n=$("#path").text();
    n=n.substring(0, n.lastIndexOf("/"));
    n=n.substring(0, n.lastIndexOf("/"))+"/";
    $.ajax({url:'/?cmd=deletefolder&folder='+encodeURIComponent(path)}).done(function(){
     $("#path").text(n);
     PATH=n;
     LoadTree(PATH);
     checkForErrors("Error deleting folder");
    });
   });
  });

  function showConfirm(text) {
   $(".main").css("filter","contrast(30%)");
   $("#dialogConfirmText").text(text);
   $("#dialogConfirm").show();
   $("#btnConfirmNo").click(function(){hideConfirm();});
  }

  function hideConfirm() {
   $(".main").css("filter","contrast(100%)");
   $("#dialogConfirm").hide();
  }

  /* Dialog */
  function showProgress(title, progressDetail) {
   $(".main").css("filter","contrast(30%)");
   $("#dialogWorking").show();
   $("#btnCancelThread").show();
   $("#progressTitle").text(title);
   $("#progressDetail").text(progressDetail);
   $(".progressbar>div").css("width","0%");
  }

  function goProgress(title,progressDetail) {
   setTimeout(updateProgress,400);
  }

  function exitProgress() {
   $(".main").css("filter","contrast(100%)");
   $("#dialogWorking").hide();
   if(NEEDSREFRESH)
    LoadTree(PATH);
  }

  function checkForErrors(title) {
   $.ajax({url:"/?cmd=geterrorlog"}).done(function(err){
   if(err && err.length>2)
    showerror(title,err);
   });
  }

  function showerror(title,err) {
   $("#dialogText").html("<b>"+title+"</b><br />"+htmlEncode(err).replace(/\n/g,"<br />"));
   $("#dialogAlert").show();
  }

  function updateProgress() {
   $.ajax({url:'/?cmd=status&t='+Math.random()}).done(function(data){
    var s=data.split("<>");
    console.log(s);
        
    if(s[5]==="end") {
     exitProgress();
     $.ajax({url:'/?cmd=geterrorlog'}).done(function(d) {
      if(d.length>=22) {
       $("#dialogText").html(htmlEncode(d).replace(/\n/g,"<br />"));
       $("#dialogAlert").show();
      }
      else {
       $.ajax({url:'/?cmd=getrsyncresult'}).done(function(d) {
        $("#dialogText").html(htmlEncode(d).replace(/\n/g,"<br />"));
        $("#dialogAlert").show();
       });
      }
     });
     return;
    }
    
    var file = s[3];
    if((file.split("/").length - 1)>=2) {
     var a=file.split("/");
     var n=a.length;
     file = a[n-2]+"/"+a[n-1];
     //file = "…/"+file;
    }
    $(".progressbar>div").css("width",s[2]+"%"); // Percent total
    if(s[6].length==0 || s[6]==='0' || s[6]==0)
     $("#progressDetail").text(file);
    else
     $("#progressDetail").text(file + " ("+s[6]+"%)"); // File + percent this file
   });
   if($("#dialogWorking").is(":visible"))
    setTimeout(updateProgress,400);
  }

  $("#btnAlertOK").click(function(){
   $("#dialogAlert").hide();   
  });
  
  $("#btnResultOk").click(function(){
   $("#dialogResult").hide();   
  });

  $("#btnCancelThread").click(function(){
   $("body").css("cursor","wait");
   $("#btnCancelThread").hide();
   $("#progressDetail").text("Cancelling...");
   $.ajax({url:'/?cmd=kill'}).done(function(data){
    exitProgress();
    $("body").css("cursor","default");
   }).fail(function(){
    exitProgress();
    $("body").css("cursor","default");
   });
  });


});
