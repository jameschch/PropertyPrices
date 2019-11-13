library(plotly)
library(htmlwidgets)
library(htmltools)
library(rvest)
library(jsonlite)

allData <- read.csv("../../PropertyPrices/UK-HPI-full-file-2019-07.csv", header = TRUE, sep = ",")

for (target in names(allData)[-(0:3)]) {

    #london = c("Barking and Dagenham", "Barnet", "Bexley", "Brent", "Bromley", "Camden", "Croydon", "Ealing", "Enfield", "Greenwich", "Hackney", "Hammersmith and Fulham", 
    #"Haringey", "Harrow", "Havering", "Hillingdon", "Hounslow", "Islington", "Kensington and Chelsea", "Kingston upon Thames", "Lambeth", "Lewisham", 
    #"Merton", "Newham", "Redbridge", "Richmond upon Thames", "Southwark", "Sutton", "Tower Hamlets", "Waltham Forest", "Wandsworth", "City of Westminster",
    #"Kensington And Chelsea", "London", "City of London")

    #target = "FlatPrice"

    propertyData <- allData[, c("Date", target, "RegionName", "AreaCode")][order(as.Date(allData$Date, format = "%d/%m/%Y")),]

    #londonOnly <- subset(propertyData, RegionName %in% london)
    londonOnly <- subset(propertyData, startsWith(as.character(AreaCode), "E09") | RegionName == "London")

    p1 = plot_ly(londonOnly, x = as.Date(londonOnly$Date, format = "%d/%m/%Y"), y = londonOnly[, target], split = londonOnly$RegionName, type = "scatter", mode = "lines")

    p1 = layout(p1, title = target, plot_bgcolor = "#000", paper_bgcolor = "#000", font = list(color = "white"))


    html <- htmlwidgets:::toHTML(p1)
    rendered <- htmltools::renderTags(html)

    content = xml_text(xml_find_all(read_html(rendered$html), "//script"))

    json = fromJSON(content)

    write(toJSON(json$x$data), paste("..\\", trimws(target), ".json", sep = ""))

}
