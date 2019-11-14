library(plotly)
library(htmlwidgets)
library(htmltools)
library(rvest)
library(jsonlite)

allData <- read.csv("C:/Source/Repos/PropertyPrices/PropertyPrices.Batcher/bin/x64/Release/netcoreapp3.0/log.txt", header = FALSE, sep = ",",)
sep = tidyr::separate(allData[,], 3, c("key", "Target"), sep = ":")
sep2 = tidyr::separate(sep[,], "V4", c("key2", "Offset"), sep = ":")
sep3 = tidyr::separate(sep2[,], "V5", c("key3", "RegionName"), sep = ":")
sep4 = tidyr::separate(sep3[,], "V6", c("key4", "London"), sep = ":")
sep5 = tidyr::separate(sep4[,], "V8", c("key4", "Next"), sep = ":")

df = sep5[, c("Target", "Offset", "RegionName", "London", "Next")]
trimmed = data.frame(lapply(df, trimws), stringsAsFactors = FALSE)

londonOnly <- subset(trimmed, London == "True")

allPlots <- htmltools::tagList()
i = 1;

for (target in split(londonOnly, f = londonOnly$Target)) {

    p1 = plot_ly(target, x = as.Date(paste(as.integer(target$Offset) + 2019, 6, 30, sep = "/")), y = round(as.double(target$Next)*100, 2), split = target$RegionName, type = "scatter", mode = "lines+markers")
    p1 = layout(p1, plot_bgcolor = "#000",  title = target$Target, paper_bgcolor = "#000", font = list(color = "white"), yaxis = list(title = "% Change"))
    allPlots[[i]] = p1
    i = i+1

    ##export
    html <- htmlwidgets:::toHTML(p1)
    rendered <- htmltools::renderTags(html)
    content = xml_text(xml_find_all(read_html(rendered$html), "//script"))
    json = fromJSON(content)
    write(toJSON(json$x$data), paste("..\\forecast\\", trimws(target$Target[1]), ".json", sep = ""))
}

allPlots[1]
allPlots[2]
allPlots[3]
allPlots[4]
allPlots[5]
allPlots[6]
allPlots[7]