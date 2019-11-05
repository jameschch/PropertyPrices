library(plotly)
library(htmlwidgets)


propertyData <- read.csv("../../PropertyPrices/UK-HPI-full-file-2019-07.csv", header = TRUE, sep = ",")

#london = c("Barking and Dagenham", "Barnet", "Bexley", "Brent", "Bromley", "Camden", "Croydon", "Ealing", "Enfield", "Greenwich", "Hackney", "Hammersmith and Fulham", 
#"Haringey", "Harrow", "Havering", "Hillingdon", "Hounslow", "Islington", "Kensington and Chelsea", "Kingston upon Thames", "Lambeth", "Lewisham", 
#"Merton", "Newham", "Redbridge", "Richmond upon Thames", "Southwark", "Sutton", "Tower Hamlets", "Waltham Forest", "Wandsworth", "City of Westminster",
#"Kensington And Chelsea", "London", "City of London")

target = "FlatPrice"

propertyData <- propertyData[, c("Date", target, "RegionName", "AreaCode")][order(as.Date(propertyData$Date, format = "%d/%m/%Y")),]

#londonOnly <- subset(propertyData, RegionName %in% london)
londonOnly <- subset(propertyData, startsWith(as.character(AreaCode), "E09") | RegionName == "London")

p1 = plot_ly(londonOnly, x = as.Date(londonOnly$Date, format = "%d/%m/%Y"), y = londonOnly[,target], split = londonOnly$RegionName, type = "scatter", mode = "lines")

p1 = layout(p1, title = target, plot_bgcolor = "#000", paper_bgcolor = "#000", font = list(color = "white"))


html <- htmlwidgets:::toHTML(p)
rendered <- htmltools::renderTags(html)

p1
