window.onload=LoadPage;
//window.onunload=Window_Unload;
window.onbeforeprint = set_to_print;
window.onafterprint = reset_form;

var scrollingPos = -1;

function registerEventHandler (element, event, handler)
{
	if (element.attachEvent) {
		// MS registration model
		element.attachEvent('on' + event, handler);
	}
	else if (element.addEventListener) {
		// NN (W4C) registration
		element.addEventListener(event, handler, false);
	}
	else {
		// old regisration model as fall-back
		element[event] = handler;
	}
}
function getInstanceDelegate (obj, methodName)
{
	return( function(e) {
		e = e || window.event;
		return obj[methodName](e);
	} );
}

function SplitScreen (nonScrollingRegionId, scrollingRegionId)
{
	this.nonScrollingRegion = document.getElementById(nonScrollingRegionId);
	this.scrollingRegion = document.getElementById(scrollingRegionId);

	document.body.style.margin = "0px";
	document.body.style.overflow = "hidden";
	this.scrollingRegion.style.overflow = "auto";

	this.resize(null);
	registerEventHandler(window, 'resize', getInstanceDelegate(this, "resize"));
	
	//try { this.scrollingRegion.SetActive(); } catch(e) {}
}
SplitScreen.prototype.resize = function(e) {
	this.scrollingRegion.style.height = document.body.clientHeight - this.nonScrollingRegion.offsetHeight;
	this.scrollingRegion.style.width = document.body.clientWidth;
}

function LoadPage()
{
	var screen = new SplitScreen('HDR', 'MAIN');

	//set the scroll position
	try { MAIN.scrollTop = scrollingPos; }
	catch(e){}

	try { HDR.setActive(); } catch(e) { }
	try { MAIN.setActive(); } catch(e) { }
}

function set_to_print()
{
	var i;
	if (window.text)document.all.text.style.height = "auto";		
	for (i=0; i < document.all.length; i++)
	{
		if (document.all[i].tagName == "body")
		{
			document.all[i].scroll = "yes";
		}
		if (document.all[i].id == "HDR")
		{
			document.all[i].style.margin = "0px 0px 0px 0px";
			document.all[i].style.width = "100%";
		}
		if (document.all[i].id == "MAIN")
		{
			document.all[i].style.overflow = "visible";
			document.all[i].style.top = "5px";
			document.all[i].style.width = "100%";
			document.all[i].style.padding = "0px 10px 0px 30px";
		}
	}
}

function reset_form()
{
	document.location.reload();
}

function loadAll()
{
	try 
	{
		scrollingPos = allHistory.getAttribute("Scroll");
	}
	catch(e){}
}

function saveAll()
{
	try
	{
		allHistory.setAttribute("Scroll", MAIN.scrollTop);
	}
	catch(e){}
}
