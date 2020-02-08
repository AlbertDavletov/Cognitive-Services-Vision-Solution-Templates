import React, { Component } from 'react';
import { Text, View, StyleSheet } from 'react-native';

const CoverageChart = (params) => {
    const { 
        chartTitlePanel, 
        chartTitle, 
        chartLabel, 
        chartBar, 
        chartTaggedItem, 
        chartUnknownItem, 
        chartShelfItem,
        chartLegend, 
        chartTaggedItemLegend, 
        chartUnknownItemLegend, 
        chartShelfItemLegend
    } = styles;
    const { title, subTitle, data } = params;
    
    let taggedItemWidth = data.taggedProductArea + '%';
    let unknownItemWidth = data.unknownProductArea + '%';
    let shelfItemWidth = data.shelfGapArea + '%';

    return (
        <View>
            <View style={chartTitlePanel}>
                <Text style={chartTitle}>{title}</Text>
                <Text style={chartLabel}>{subTitle}</Text>
            </View>

            <View style={chartBar}>
                <View style={[chartTaggedItem, { width: taggedItemWidth }]}>
                    <Text numberOfLines={1} style={{color: 'white'}}>{taggedItemWidth}</Text>
                </View>
                <View style={[chartUnknownItem, { width: unknownItemWidth }]}>
                    <Text numberOfLines={1} style={{color: 'white'}}>{unknownItemWidth}</Text>
                </View>
                <View style={[chartShelfItem, { width: shelfItemWidth }]}>
                    <Text numberOfLines={1} style={{color: 'black'}}>{shelfItemWidth}</Text>
                </View>
            </View>

            <View style={{flexDirection: 'row'}}>
                <View style={chartLegend}>
                    <View style={chartTaggedItemLegend} />
                    <Text style={chartLabel}>Tagged item</Text>
                </View>
                <View style={[chartLegend, { marginLeft: 12, marginRight: 12}]}>
                    <View style={chartUnknownItemLegend} />
                    <Text style={chartLabel}>Unknown item</Text>
                </View>
                <View style={chartLegend}>
                    <View style={chartShelfItemLegend} />
                    <Text style={chartLabel}>Shelf gap</Text>
                </View>
            </View>
        </View>
    )
}

const styles = StyleSheet.create({
    chartTitlePanel: {
        flexDirection: 'row', 
        justifyContent: 'space-between',
    },
    chartTitle: {
        color: 'white',
        opacity: 0.8,
        fontSize: 13
    },
    chartLabel: {
        color: 'white',
        fontSize: 11,
        opacity: 0.6
    },
    chartBar: {
        flexDirection: 'row', 
        height: 25,
        marginTop: 9,
        marginBottom: 12,
    },
    chartTaggedItem: {
        backgroundColor: '#248FFF', 
        alignItems: 'center', 
        justifyContent: 'center',
    },
    chartUnknownItem: {
        backgroundColor: '#B4009E',
        alignItems: 'center', 
        justifyContent: 'center',
        width: '25%',
    },
    chartShelfItem: {
        backgroundColor: '#FABE14',
        alignItems: 'center',
        justifyContent: 'center',
        width: '5%'
    },
    chartLegend: {
        flexDirection: 'row', 
        alignItems: 'center'
    },
    chartTaggedItemLegend: {
        backgroundColor: '#248FFF', 
        height: 8, 
        width: 8, 
        marginRight: 4
    },
    chartUnknownItemLegend: {
        backgroundColor: '#B4009E',
        height: 8, 
        width: 8, 
        marginRight: 4
    },
    chartShelfItemLegend: {
        backgroundColor: '#FABE14',
        height: 8, 
        width: 8, 
        marginRight: 4
    }
})

export { CoverageChart };