import React from 'react'
import { View, Text } from 'react-native'
import { NavigationScreenProp, NavigationState, NavigationParams } from 'react-navigation'
import { CoverageChart, ResultTable } from '../../components'
import { TableData, ProductItem, SpecData, SpecItem } from '../../models'
import { styles } from './ResultScreen.style'
import { UnknownProduct, ShelfGap } from '../../../constants'

interface ResultScreenProps {
    navigation: NavigationScreenProp<NavigationState, NavigationParams>;
}

interface ResultScreenState {
    tableData: Array<TableData>;
    chartData: {
        totalItems: string,
        taggedProductArea: string,
        unknownProductArea: string,
        shelfGapArea: string
    }
}

export class ResultScreen extends React.Component<ResultScreenProps, ResultScreenState> {
    constructor(props: ResultScreenProps) {
        super(props);
        this.state = {
            tableData: Array<TableData>(),
            chartData: {
                totalItems: '0',
                taggedProductArea: '0',
                unknownProductArea: '0',
                shelfGapArea: '0'
            }
        }
    }

    async componentDidMount() {
        const { navigation } = this.props;
        let data = navigation.getParam('data', Array<ProductItem>());
        let specData = navigation.getParam('specData', {});

        this.updateChart(data);
        this.updateResultTable(data, specData);
    }

    render() {
        let chartSubtitle = this.state.chartData.totalItems + ' total items';

        return (
            <View style={styles.mainContainer}>
                <CoverageChart 
                    title='Share of Shelf by area' 
                    subTitle={chartSubtitle}
                    data={this.state.chartData}
                />

                <View style={{ marginTop: 15, marginBottom: 15 }}>
                    <Text style={{ color: 'white', opacity: 0.8 }}>Shelf audit detail</Text>
                </View>

                <View style={{ flex: 1, marginTop: 10 }}>
                    <ResultTable data={this.state.tableData}/>
                </View>                
            </View>
        );
    }

    updateChart(data: Array<ProductItem>) {

        let totalArea = 0;
        let unknownProductArea = 0;
        let shelfGapArea = 0;
        let taggedProductArea = 0;

        data.forEach((p: ProductItem) => {
            let pArea = p.model.boundingBox.width * p.model.boundingBox.height;
            totalArea += pArea;

            const isUnknownProduct = p.displayName.toLocaleLowerCase() === UnknownProduct.toLocaleLowerCase();
            const isShelfGap = p.displayName.toLocaleLowerCase() === ShelfGap.toLocaleLowerCase();
            if (isUnknownProduct) {
                unknownProductArea += pArea;
            } else if (isShelfGap) {
                shelfGapArea += pArea;
            }
        });
        taggedProductArea = totalArea - unknownProductArea - shelfGapArea;

        const taggedProductAreaPerc = (totalArea > 0 ? 100 * taggedProductArea / totalArea : 0).toFixed(2);
        const unknownProductAreaPerc = (totalArea > 0 ? 100 * unknownProductArea / totalArea : 0).toFixed(2);
        const shelfGapAreaPerc = (totalArea > 0 ? 100 * shelfGapArea / totalArea : 0).toFixed(2);

        this.setState({
            chartData: {
                totalItems: data.length.toString(),
                taggedProductArea : taggedProductAreaPerc,
                unknownProductArea: unknownProductAreaPerc,
                shelfGapArea: shelfGapAreaPerc
            }
        })
    }

    updateResultTable(data: Array<ProductItem>, specData: SpecData) {
        const groupByName = this.groupBy('displayName');
        const imageBaseUrl = specData.CanonicalImages;
        const tagsByName = groupByName(data);

        let tableData = Array<TableData>();
        for (let name in tagsByName) {
            const products = tagsByName[name];
            const totalCount = products.length;

            if (totalCount > 0) {
                const model = products[0].model;
                const tagId = model.tagId;
                const specItem = specData.Items.find((item: SpecItem) => {
                    return item.TagId === tagId;
                });

                const expectedCount = specItem != null ? specItem.ExpectedCount : 0;
                tableData.push({
                    Name: name,
                    TotalCount: totalCount,
                    ExpectedCount: expectedCount,
                    ImageUrl: imageBaseUrl + name.toLocaleLowerCase() + '.jpg'
                });
            }
        }

        this.setState({
            tableData: tableData
        });
    }

    groupBy = (key: any) => (array: Array<any>) =>
        array.reduce((objectsByKeyValue, obj) => {
            const value = obj[key];
            objectsByKeyValue[value] = (objectsByKeyValue[value] || []).concat(obj);
            return objectsByKeyValue;
        }, 
    {});
}
