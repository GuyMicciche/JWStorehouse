var item;
var highlight;

function PageOnLoad() {
    alert("Hello");
}

function DisplayLearningDialog(item) {
    this.item = item;

    var chinese;
    var pinyin;
    var english;

    if (item.firstChild.nextSibling.firstChild.childElementCount == null) {
        chinese = item.firstChild.nextSibling.firstChild.nodeValue;
        pinyin = item.firstChild.firstChild.nodeValue;
        english = item.getAttribute("title");
    }
    else {
        chinese = item.firstChild.nextSibling.firstChild.nextSibling.firstChild.nodeValue;
        pinyin = item.firstChild.nextSibling.firstChild.firstChild.nodeValue;
        english = item.getAttribute("title");
    }

    KingJavaScriptInterface.DisplayLearningDialog(chinese, pinyin, english);
}

function ToggleEnglish() {
    var item = this.item;
    var elements = GetAllElementsWithAttributeValue('title', item.getAttribute("title"));

    if (item.firstChild.nextSibling.firstChild.childElementCount == null) {

        for (var i = 0; i < elements.length; i++)
        {
            var elm = elements[i];

            var html = "<rt style='color:gray;font-size:0.7em;display:compact;'>";
            html += elm.getAttribute("title");
            html += '</rt><ruby>';
            html += elm.innerHTML;
            html += '</ruby>';

            elm.innerHTML = html;
        }
    }
    else {
        for (var i = 0; i < elements.length; i++) {
            var elm = elements[i];

            var html = elm.innerHTML;
            var pattern = /(<rt style)(.*?)(<\/rt>)(<ruby>)(.*?)(<\/ruby>)/g;
            var match = pattern.exec(html);
            html = match[5];

            elm.innerHTML = html;
        }
    }
}

function GetAllElementsWithAttributeValue(attribute, value) {
    var matchingElements = [];
    var allElements = document.getElementsByTagName('*');
    for (var i = 0, n = allElements.length; i < n; i++) {
        if (allElements[i].getAttribute(attribute) !== null && allElements[i].getAttribute(attribute) == value) {
            matchingElements.push(allElements[i]);
        }
    }
    return matchingElements;
}

function SetSelectedTextInfo() {
    var selectedText = '';
    var begin;
    var end;

    if (document.getSelection) {
        begin = document.selectionStart;
        selectedText = document.getSelection();
        end = document.selectionEnd;
    }
    else if (window.getSelection) {
        selectedText = window.getSelection();
        begin = GetTextPosition(windiow);
        end = begin + selectedText.length;
    }
    else if (document.selection) {
        selectedText = document.selection.createRange().text;
        begin = GetTextPosition(document);
        end = begin + selectedText.length;
    }
    else {
        return;
    }

    var SelectedTextInfo = new Object();
    SelectedTextInfo.selectedText = selectedText;
    SelectedTextInfo.begin = begin;
    SelectedTextInfo.end = end;

    highlight = SelectedTextInfo;
}

function GetTextPosition() {
    var iCaretPos = 0;
    if (document.selection) {
        var range = document.selection.createRange();
        var stored_range = range.duplicate();
        stored_range.moveToElementText(document);
        stored_range.setEndPoint('EndToEnd', range);
        iCaretPos = stored_range.text.length - range.text.length;
    }
    else if (document.selectionStart || document.selectionStart == '0') {
        iCaretPos = document.selectionStart;
    }

    return (iCaretPos);
}

function GetSelectedText()
{
    if (window.getSelection)
    {
        return window.getSelection();
    }
    else if (document.getSelection)
    {
        return document.getSelection();
    }
    else if (document.selection)
    {
        return document.selection.createRange().text;
    }
    else return;
}

function HighlightSelection()
{
    var userSelection = highlight.selectedText.getRangeAt(0);
    var safeRanges = GetSafeRanges(userSelection);
    for (var i = 0; i < safeRanges.length; i++)
    {
        HighlightRange(safeRanges[i]);
    }

    KingJavaScriptInterface.StoreHighlight(highlight.selectedText, highlight.begin, highlight.end);
}

function HighlightRange(range)
{
    var newNode = document.createElement("div");
    newNode.setAttribute("id", "highlight");
    newNode.setAttribute("class", "highlight-color");
    range.surroundContents(newNode);
}

function ClearHighlights()
{
    var nodesToBeRemoved = document.getElementsByClassName("highlight-color");
    while (nodesToBeRemoved.length > 0)
    {
        nodesToBeRemoved = document.getElementsByClassName("highlight-color");

        var node = nodesToBeRemoved[0];
        while (node.firstChild)
        {
            node.parentNode.insertBefore(node.firstChild, node);
        }
        node.parentNode.removeChild(node);
    }
}


function GetSafeRanges(dangerous)
{
    var a = dangerous.commonAncestorContainer;
    var s = new Array(0), rs = new Array(0);
    if (dangerous.startContainer != a)
    {
        for (var i = dangerous.startContainer; i != a; i = i.parentNode)
        {
            s.push(i)
        }
    }    
    if (0 < s.length) for (var i = 0; i < s.length; i++)
        {
        var xs = document.createRange();
        if (i)
        {
            xs.setStartAfter(s[i - 1]);
            xs.setEndAfter(s[i].lastChild);
        }
        else
        {
            xs.setStart(s[i], dangerous.startOffset);
            xs.setEndAfter((s[i].nodeType == Node.TEXT_NODE) ? s[i] : s[i].lastChild);
        }
        rs.push(xs);
    }

    var e = new Array(0), re = new Array(0);
    if (dangerous.endContainer != a)
    {
        for (var i = dangerous.endContainer; i != a; i = i.parentNode)
        {
            e.push(i) 
        }
    }
    if (0 < e.length) for (var i = 0; i < e.length; i++)
        {
        var xe = document.createRange();
        if (i)
        {
            xe.setStartBefore(e[i].firstChild);
            xe.setEndBefore(e[i - 1]);
        }
        else
        {
            xe.setStartBefore((e[i].nodeType == Node.TEXT_NODE) ? e[i] : e[i].firstChild);
            xe.setEnd(e[i], dangerous.endOffset);
        }
        re.unshift(xe);
    }

    if ((0 < s.length) && (0 < e.length))
    {
        var xm = document.createRange();
        xm.setStartAfter(s[s.length - 1]);
        xm.setEndBefore(e[e.length - 1]);
    }
    else
    {
        return [dangerous];
    }

    rs.push(xm);
    response = rs.concat(re);

    return response;
}