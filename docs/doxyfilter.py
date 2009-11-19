#
# Filter program for use with Doxygen. Replaces NDoc XML formatting
# conventions with Doxygen formatting conventions.
import sys;
import string;
import re;

#------------------------------------------------------------------------
# If the first field is 0, then we are using a simple search
# and replace. If it is 1 then we are doing regular expression
# matching and replacement. The final item should always be
# None.
replaceDoc = [
    # simple string substitutions
    [0, "<summary>",      "<p>",            None],
    [0, "</summary>",     "</p>",           None],
    [0, "<remarks>",      "<p>",            None], # TBD: follows params
    [0, "</remarks>",     "</p>",           None],
    [0, "<example>",      "\code",          None],
    [0, "</example>",     "\endcode",       None],
    [0, "<overloads>",    "<p>",            None],
    [0, "</overloads>",   "</p>",           None],
    [0, "<value>",        "<p>",            None],
    [0, "</value>",       "</p>",           None],
    [0, "<para>",         "<p>",            None],
    [0, "<para/>",        "<p/>",           None],
    [0, "</para>",        "</p>",           None],
    [0, "<code>",         "\code ",         None], # TBD: extra junk
    [0, "</code>",        "\endcode ",      None],
    [0, "<c>",            r"<code>",        None],
    [0, "</c>",           r"</code>",       None],
    [0, "<description>",  "",               None],
    [0, "</description>", "",               None],
    [0, "<returns>",      "@return ",       None],
    [0, "</returns>",     "",               None],
    [0, "</param>",       "",               None],
    [0, "<item>",         "<li>",           None],
    [0, "</item>",        "</li>",          None],
    #[0, "<p/>",         "\par ",          None],
    [0, "<term>",         "<b>",            None],
    [0, "</term>",        "</b>",           None],
    [0, "</list>",        "</ul>",          None],
    [0, "</see>",         "",               None],
    [0, "<exception>",    "<p/>",           None], # degenerate form
    [0, "</exception>",   "",               None],
    [0, "</paramref>",    "</b>",           None],
    # TBD: Anything smart we can do with "/// ---"?
           
    # regular expression substitutions
    [1, "<param\\s+name=\"([^\"]+)\">",       r"@param \1 ",     None],
    [1, "<paramref\\s+name=\"([^\"]+)\">",    r"<b>\1",     None],
    #[1, "<list\\s+type=\"bullet\">",          "<ul>",            None],
    #[1, "<list\\s+type=\"number\">",          "<ol>",            None],
    # correctly terminating different lists depends on context we don't have!
    [1, "<list.*>",                           "<ul>",            None],
    [1, "<see\\s+cref=\"([^\"]+)\"/?>",       r" \1 ",           None],
    [1, "<exception\\s+cref=\"([^\"]+)\"/?>", r"\exception \1 ", None],
];


#------------------------------------------------------------------------
replaceCode = [
    # simple string substitutions
    [0, "August.",        "",               None],
];


#------------------------------------------------------------------------
# Procedure to apply all text transformations
def transformText(xforms, line):
    # go through and do substitutions appropriate to code
    for subInfo in xforms:
        if not subInfo[0]:
            # apply simple search and replace
            line = string.replace(line, subInfo[1], subInfo[2]);
        else:
            if subInfo[3] == None:
                # first time -- setup regex object
                subInfo[3] = re.compile(subInfo[1]);
            # apply regular expression search and replace
            #line = re.sub(subInfo[3], subInfo[2], line); 
            line = subInfo[3].sub(subInfo[2], line); 
    return line;


#------------------------------------------------------------------------
# open up the file and go through line by line
fileName   = sys.argv[1];
file       = open(fileName, "r");
line       = file.readline();
blockStart = re.compile("^\s*///");
findSlash  = re.compile("//+/");
descAttr   = re.compile("Description\(\"([^\"]+)\"\)");
while line:
    # if this line is the start of a ///-style comment block, read in
    # the whole block and convert to /* */-style
    if blockStart.search(line) != None:
        # read in the full comment block, modifying as we go
        block = [];
        sub   = "/** ";
        while line and blockStart.search(line) != None:
            line = findSlash.sub(sub, line);
            sub  = "   ";
            string.expandtabs(line);
            block.append(line);
            line = file.readline();
        block[-1] = string.replace(block[-1], "\n", " */\n");
        
        # special case -- no comment block because the Description
        # attribute is being used
        if len(block) == 1 and string.find(block[0], "---") >= 0:
            #print " /* oh yeah */ ";
            desc = descAttr.search(line);
            if desc:
                #print " /* replacing */ ";
                block[0] = "/** %s */\n" % desc.group(1);
        
        # special case -- triple dash indicating undocumented or
        # documented in parent
        if len(block) == 1 and string.find(block[0], "---") >= 0:
            block[0] = "/** Documented in parent class or undocumented... */\n";
        
        # now go through each line and do the string substitutions
        # appropriate for documentation text
        for ln in block:
            ln = transformText(replaceDoc, ln);
            print ln,;
            
    if line:
        # go through and do substitutions appropriate to code
        line = transformText(replaceCode, line);
        print line,;
    line = file.readline();
    
file.close();
