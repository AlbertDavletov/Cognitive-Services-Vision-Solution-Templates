import React, { Component } from 'react';
import { View, Text, StyleSheet, } from 'react-native';
import { CoverageChart, ResultTable } from '../components/uikit';

class ResultScreen extends React.Component {
    constructor(props) {
        super(props);
        this.state = {
            tableData: [],
            chartData: {
                totalItems: 0,
                taggedProductArea: 0,
                unknownProductArea: 0,
                shelfGapArea: 0
            }
        }
    }

    async componentDidMount() {
        const { navigation } = this.props;
        let data = navigation.getParam('data', []);
        let specData = navigation.getParam('specData', []);

        this.updateChart(data);
        this.updateResultTable(data, specData);
    }

    render() {
        const { navigation } = this.props;
        const { mainContainer, h1, } = this.styles;
        let chartSubtitle = this.state.chartData.totalItems + ' total items';

        return (
            <View style={mainContainer}>
                <CoverageChart 
                    title='Share of Shelf by area' 
                    subTitle={chartSubtitle}
                    data={this.state.chartData}
                />

                <View style={{ marginTop: 15, marginBottom: 15 }}>
                    <Text style={{ color: 'white', opacity: 0.8 }}>Shelf audit detail</Text>
                </View>

                <View style={{ flex: 1, marginTop: 10,  }}>
                    <ResultTable data={this.state.tableData}/>
                </View>

                {/* <View style={{ backgroundColor: 'gray' }}>
                    <Text>Test</Text>
                </View> */}
                
            </View>
        );
    }

    updateChart(data) {

        let totalArea = 0;
        let unknownProductArea = 0;
        let shelfGapArea = 0;
        let taggedProductArea = 0;

        data.forEach(p => {
            let pArea = p.model.boundingBox.width * p.model.boundingBox.height;
            totalArea += pArea;
            if (p.displayName.toLocaleLowerCase() == 'product') {
                unknownProductArea += pArea;
            } else if (p.displayName.toLocaleLowerCase() == 'gap') {
                shelfGapArea += pArea;
            }
        });
        taggedProductArea = totalArea - unknownProductArea - shelfGapArea;

        let taggedProductAreaPerc = (totalArea > 0 ? 100 * taggedProductArea / totalArea : 0).toFixed(2);
        let unknownProductAreaPerc = (totalArea > 0 ? 100 * unknownProductArea / totalArea : 0).toFixed(2);
        let shelfGapAreaPerc = (totalArea > 0 ? 100 * shelfGapArea / totalArea : 0).toFixed(2);

        this.setState({
            chartData: {
                totalItems: data.length,
                taggedProductArea : taggedProductAreaPerc,
                unknownProductArea: unknownProductAreaPerc,
                shelfGapArea: shelfGapAreaPerc
            }
        })
    }

    updateResultTable(data, specData) {
        const groupByName = this.groupBy('displayName');
        let tagsByName = groupByName(data);

        let tableData = [];
        for (let name in tagsByName) {
            let products = tagsByName[name];
            let totalCount = products.length;
            if (totalCount > 0) {
                let model = products[0].model;
                let tagId = model.tagId;
                let specItem = specData.Items.find(item => {
                    return item.TagId == tagId;
                });

                let expectedCount = specItem != null ? specItem.ExpectedCount : 0;

                tableData.push({
                    Name: name,
                    TotalCount: totalCount,
                    ExpectedCount: expectedCount,
                    ImageUrl: 'https://intelligentkioskstore.blob.core.windows.net/shelf-auditing/Mars/Products/' + name.toLocaleLowerCase() + '.jpg'
                });
            }
        }

        this.setState({
            tableData: tableData
        });
    }

    groupBy = key => array =>
        array.reduce((objectsByKeyValue, obj) => {
            const value = obj[key];
            objectsByKeyValue[value] = (objectsByKeyValue[value] || []).concat(obj);
            return objectsByKeyValue;
        }, 
    {}); 

    styles = StyleSheet.create({
        mainContainer: {
            flex: 1,
            backgroundColor: 'black',
            padding: 16
        },
        h1: {
            color: 'white',
            fontSize: 17,
            padding: 20
        },
        
    })
}

export { ResultScreen };