import React from 'react'
import { Text, View } from 'react-native'
import { styles } from './CoverageChart.style'

interface ChartProps {
    title: string;
    subTitle: string;
    data: any;
}

export const CoverageChart = (params: ChartProps) => {
    const { title, subTitle, data } = params;
    
    let taggedItemWidth = data.taggedProductArea + '%';
    let unknownItemWidth = data.unknownProductArea + '%';
    let shelfItemWidth = data.shelfGapArea + '%';

    return (
        <View>
            <View style={styles.chartTitlePanel}>
                <Text style={styles.chartTitle}>{title}</Text>
                <Text style={styles.chartLabel}>{subTitle}</Text>
            </View>

            <View style={styles.chartBar}>
                <View style={[styles.chartTaggedItem, { width: taggedItemWidth }]}>
                    <Text numberOfLines={1} style={{color: 'white'}}>{taggedItemWidth}</Text>
                </View>
                <View style={[styles.chartUnknownItem, { width: unknownItemWidth }]}>
                    <Text numberOfLines={1} style={{color: 'white'}}>{unknownItemWidth}</Text>
                </View>
                <View style={[styles.chartShelfItem, { width: shelfItemWidth }]}>
                    <Text numberOfLines={1} style={{color: 'black'}}>{shelfItemWidth}</Text>
                </View>
            </View>

            <View style={{flexDirection: 'row'}}>
                <View style={styles.chartLegend}>
                    <View style={styles.chartTaggedItemLegend} />
                    <Text style={styles.chartLabel}>Tagged item</Text>
                </View>
                <View style={[styles.chartLegend, { marginLeft: 12, marginRight: 12}]}>
                    <View style={styles.chartUnknownItemLegend} />
                    <Text style={styles.chartLabel}>Unknown item</Text>
                </View>
                <View style={styles.chartLegend}>
                    <View style={styles.chartShelfItemLegend} />
                    <Text style={styles.chartLabel}>Shelf gap</Text>
                </View>
            </View>
        </View>
    )
}
